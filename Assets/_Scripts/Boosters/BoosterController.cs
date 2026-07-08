using System;
using System.Collections;
using System.Collections.Generic;
using PuppyPuzzle.Ads;
using PuppyPuzzle.Economy;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace PuppyPuzzle.Boosters
{
    /// <summary>
    /// All booster functionality in one place: the tap/spend/ad flow AND every
    /// booster's actual effect. The <see cref="Booster"/> components on the bottom-bar
    /// buttons only carry their type and visual state (see BoosterUIController).
    /// Counts persist in <see cref="EconomyHandler"/> (legacy "powerup_chances_"
    /// keys — old saves carry over).
    ///
    /// Use flow for a tap (see <see cref="TryUseBooster"/>):
    ///   locked            → ignored (button is disabled anyway)
    ///   has booster count → consume one and activate (refund on failure)
    ///   count == 0        → spend coins to use once (coins refunded on failure)
    ///   coins short       → rewarded ad (stub service) → activate on reward
    ///
    /// REVEAL: auto-places one guaranteed-correct puppy via GameManager's solver.
    ///
    /// HINT: a progressive guided-deduction hint shown through the shared popup
    /// system (<see cref="HintPopup"/> via <see cref="PopupCanvasManager"/>).
    /// Each press picks the first applicable step, in priority order:
    ///
    ///  1. INVALIDATION — fresh hidden-solution board: previews X marks on every
    ///     cell that is not part of the solution. Apply commits them.
    ///  2. ELIMINATION — the most recently placed puppy that still has UNMARKED
    ///     forbidden cells (its row, column, diagonal neighbours): previews X marks
    ///     on those cells. Apply commits them. Pressing again walks back through
    ///     older puppies (recent → previous → initial/given).
    ///  3. FORCED LINE — a row or column whose only remaining un-excluded cell can
    ///     legally hold a puppy: highlights it ("Only one cell left in Row/Column N").
    ///     Apply places the puppy there.
    ///  4. SOLVER FALLBACK — nothing above applies: the solver picks the next
    ///     solution cell and highlights it. Apply places the puppy.
    ///
    /// Dismissing the hint popup without applying refunds the booster for the
    /// X-preview steps (1–2); the placement steps (3–4) keep the cost.
    /// </summary>
    [DefaultExecutionOrder(-100)] // push the config's InitialBoosters into EconomyHandler before any UI reads counts
    public class BoosterController : MonoBehaviour
    {
        public static BoosterController Instance { get; private set; }

        [Tooltip("Per-booster tuning: unlock level, coin price, and the initial booster count.")]
        [SerializeField] private BoosterConfigSO config;

        [Header("Hint Messages")]
        [TextArea]
        [Tooltip("Elimination step: previewing X marks around a placed puppy.")]
        [SerializeField] private string hintExclusionMessage =
            "This puppy's row, column and neighbors can't have other puppies — exclude them";
        [TextArea]
        [Tooltip("Forced-line step for a column. {0} = 1-based column number.")]
        [SerializeField] private string hintOnlyCellInColumnMessage =
            "Only one cell left in Column {0} for a puppy";
        [TextArea]
        [Tooltip("Forced-line step for a row. {0} = 1-based row number.")]
        [SerializeField] private string hintOnlyCellInRowMessage =
            "Only one cell left in Row {0} for a puppy";
        [TextArea]
        [Tooltip("Invalidation step: fresh hidden-solution board, marking every non-solution cell.")]
        [SerializeField] private string hintInvalidationMessage =
            "These cells can't hold a puppy — exclude them";
        [TextArea]
        [Tooltip("Solver-fallback step: everything else is marked, a puppy must go here.")]
        [SerializeField] private string hintDeductionMessage =
            "By elimination, a puppy must go in the highlighted cell";

        [Header("Hint Timing")]
        [Tooltip("Stagger between each ghost-X / commit animation for a cascading feel.")]
        [Min(0f)]
        [SerializeField] private float hintStagger = 0.04f;

        /// <summary>Raised whenever a booster's remaining count changes (type, newCount).</summary>
        public event Action<BoosterType, int> OnBoosterCountChanged;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (config == null)
                Debug.LogWarning("[BoosterController] No BoosterConfigSO assigned — using fallback unlock levels/prices.", this);

            EconomyHandler.DefaultBoosterCount = config != null
                ? config.InitialBoosters
                : BoosterConfigSO.FallbackInitialBoosters;
            EconomyHandler.OnBoosterChanged += HandleBoosterChanged;
            SceneManager.sceneUnloaded += HandleSceneUnloaded;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                EconomyHandler.OnBoosterChanged -= HandleBoosterChanged;
                SceneManager.sceneUnloaded -= HandleSceneUnloaded;
                Instance = null;
            }
        }

        private void HandleSceneUnloaded(Scene scene)
        {
            // This controller outlives the Game scene; tear an open hint down so
            // GameManager.InputLocked and the popup never leak into the next scene.
            if (_hintActive) ExitHint();
        }

        // ---- Config (BoosterConfigSO-backed) ----

        /// <summary>Normal level number at which the booster unlocks.</summary>
        public int GetUnlockLevel(BoosterType type) =>
            config != null ? config.GetUnlockLevel(type) : BoosterConfigSO.FallbackUnlockLevel;

        /// <summary>Coin price for a single use when the player has no boosters left.</summary>
        public int GetCoinPrice(BoosterType type) =>
            config != null ? config.GetCoinPrice(type) : BoosterConfigSO.FallbackCoinPrice;

        // ---- Counts (EconomyHandler-backed) ----

        public int GetCount(BoosterType type) => EconomyHandler.GetBoosterCount(type);
        public bool HasCount(BoosterType type) => EconomyHandler.HasBooster(type);
        public void Grant(BoosterType type, int count = 1) => EconomyHandler.GrantBooster(type, count);

        // ---- Unlocking ----

        /// <summary>True once the player's Normal-level progression reaches the booster's unlock level.</summary>
        public bool IsUnlocked(Booster booster)
        {
            if (booster == null) return false;
            int playerLevel = LevelLoader.Instance != null ? LevelLoader.Instance.CurrentNormalLevelNumber : 1;
            return playerLevel >= booster.UnlockAtLevel;
        }

        // ---- Use flow ----

        /// <summary>
        /// Runs the full tap flow for a booster button. Returns true if the booster
        /// was activated (or an ad was opened on its behalf).
        /// </summary>
        public bool TryUseBooster(Booster booster)
        {
            if (booster == null || !IsUnlocked(booster))
                return false;

            BoosterType type = booster.Type;

            // 1) Spend a stored booster (optimistically; refund if it couldn't act).
            if (EconomyHandler.TryConsumeBooster(type))
            {
                if (!ActivateBooster(type))
                {
                    EconomyHandler.GrantBooster(type);
                    Debug.Log($"[BoosterController] {type} could not act right now — booster refunded.");
                    return false;
                }
                return true;
            }

            // 2) Out of boosters → pay coins to use once.
            if (EconomyHandler.TrySpendCoins(booster.CoinPrice))
            {
                if (!ActivateBooster(type))
                {
                    EconomyHandler.AddCoins(booster.CoinPrice);
                    Debug.Log($"[BoosterController] {type} could not act right now — coins refunded.");
                    return false;
                }
                return true;
            }

            // 3) Coins short → rewarded ad grants a single use.
            if (DummyRewardedAdService.Instance != null)
            {
                DummyRewardedAdService.Instance.ShowRewardedAd(
                    onReward: () =>
                    {
                        // Ad watched = one use. If the board state blocks it, bank a
                        // count instead so the watch is never wasted.
                        if (!ActivateBooster(type))
                            EconomyHandler.GrantBooster(type);
                    },
                    onSkip: null);
                return true;
            }

            Debug.LogWarning("[BoosterController] No ad service in scene; cannot offer rewarded use.");
            return false;
        }

        /// <summary>
        /// Fires a booster's effect immediately (cost handling is the caller's job).
        /// Returns false when the effect cannot act on the current board state.
        /// </summary>
        public bool ActivateBooster(BoosterType type)
        {
            switch (type)
            {
                case BoosterType.Hint: return TryActivateHint();
                case BoosterType.Reveal: return TryActivateReveal();
                default:
                    Debug.LogWarning($"[BoosterController] No effect implemented for booster type {type}.");
                    return false;
            }
        }

        private void HandleBoosterChanged(BoosterType type, int newCount)
        {
            OnBoosterCountChanged?.Invoke(type, newCount);
        }

        // ================= Reveal booster =================

        /// <summary>Auto-places one guaranteed-correct puppy from the solver.</summary>
        private bool TryActivateReveal()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[BoosterController] No GameManager in scene — Reveal cannot act.");
                return false;
            }

            // Returns false if the board is finished or can't be solved from here —
            // the caller then refunds the spent cost.
            return GameManager.Instance.RevealCorrectPuppy();
        }

        // ================= Hint booster =================

        private enum HintMode { Elimination, Invalidation, Placement }

        /// <summary>Everything one hint step needs, computed before any board/UI mutation
        /// so a popup failure can bail out cleanly (and refund the booster).</summary>
        private sealed class HintPlan
        {
            public HintMode Mode;
            public string Message;
            public HashSet<Vector2Int> BrightCells;   // cells kept undimmed
            public List<Cell> PreviewCells;           // cells that get ghost X's (elimination/invalidation)
            public Cell FocusCell;                    // glowing cell (puppy or placement target)
            public Vector2Int PlacementTarget;        // where Apply places a puppy (Placement mode)
        }

        // Cells touched this hint, so we can restore them on exit.
        private readonly List<Cell> _dimmedCells = new();
        private readonly List<Cell> _previewCells = new();
        private Cell _focusCell;
        private bool _hintActive;
        private HintMode _hintMode;
        private Vector2Int _placementTarget;

        // The currently-running staggered preview/apply coroutine, tracked so it can be
        // stopped on teardown (otherwise it keeps touching cells after the hint closes).
        private Coroutine _hintRoutine;

        private bool TryActivateHint()
        {
            if (_hintActive) return false; // a hint is already on screen

            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Grid == null) return false;

            if (PopupCanvasManager.Instance == null)
            {
                Debug.LogWarning("[BoosterController] No PopupCanvasManager — cannot show the hint popup.");
                return false;
            }

            HintPlan plan = BuildHintPlan(gm);
            if (plan == null)
                return false; // nothing left to teach → caller refunds the cost

            HintPopup popup = PopupCanvasManager.Instance.Show<HintPopup>(PopupType.Hint);
            if (popup == null)
            {
                Debug.LogError("[BoosterController] No HintPopup registered for PopupType.Hint in PopupsConfig.");
                return false; // board untouched → clean refund
            }

            ExecuteHint(gm, plan, popup);
            return true;
        }

        // ---- Plan building (pure reads, no board mutation) ----

        private HintPlan BuildHintPlan(GameManager gm)
        {
            int w = gm.GridWidth;
            int h = gm.GridHeight;

            // 1) Fresh hidden-solution board → mark all non-solution cells.
            IReadOnlyList<Vector2Int> puppies = gm.PlacementOrder;
            if (puppies.Count == 0 && gm.HasHiddenSolution)
            {
                var invalidCells = new List<Cell>();
                var invalidSet = new HashSet<Vector2Int>();
                for (int x = 0; x < w; x++)
                {
                    for (int y = 0; y < h; y++)
                    {
                        var pos = new Vector2Int(x, y);
                        if (gm.IsHiddenSolutionCell(pos))
                            continue;

                        Cell c = gm.Grid.GetCell(x, y);
                        if (c != null && c.CanReceiveHint)
                        {
                            invalidCells.Add(c);
                            invalidSet.Add(pos);
                        }
                    }
                }

                if (invalidCells.Count > 0)
                {
                    return new HintPlan
                    {
                        Mode = HintMode.Invalidation,
                        Message = hintInvalidationMessage,
                        BrightCells = invalidSet,
                        PreviewCells = invalidCells,
                    };
                }
            }

            // 2) Walk puppies newest-first; act on the first one with unmarked cells.
            for (int i = puppies.Count - 1; i >= 0; i--)
            {
                Vector2Int puppyPos = puppies[i];
                Cell focus = gm.Grid.GetCell(puppyPos.x, puppyPos.y);
                if (focus == null) continue;

                List<Vector2Int> restricted = GameRules.GetRuleRestrictedCells(puppyPos, w, h);
                var unmarked = new List<Cell>();
                foreach (Vector2Int rp in restricted)
                {
                    Cell c = gm.Grid.GetCell(rp.x, rp.y);
                    if (c != null && c.CanReceiveHint && !c.IsXMarked)
                        unmarked.Add(c);
                }

                if (unmarked.Count == 0) continue; // this puppy is fully marked; try an older one

                var bright = new HashSet<Vector2Int>(restricted) { puppyPos };
                return new HintPlan
                {
                    Mode = HintMode.Elimination,
                    Message = hintExclusionMessage,
                    BrightCells = bright,
                    PreviewCells = unmarked,
                    FocusCell = focus,
                };
            }

            // 3) A row/column with a single legal cell left → a puppy must go there.
            if (TryFindForcedLineCell(gm, w, h, out Vector2Int forced, out string lineMessage))
                return BuildPlacementPlan(gm, forced, lineMessage);

            // 4) Everything derivable is marked → let the solver point at the next cell.
            if (gm.TryGetNextSolutionCell(out Vector2Int target))
                return BuildPlacementPlan(gm, target, hintDeductionMessage);

            return null;
        }

        private static HintPlan BuildPlacementPlan(GameManager gm, Vector2Int target, string message)
        {
            return new HintPlan
            {
                Mode = HintMode.Placement,
                Message = message,
                BrightCells = new HashSet<Vector2Int> { target },
                FocusCell = gm.Grid.GetCell(target.x, target.y),
                PlacementTarget = target,
            };
        }

        /// <summary>
        /// Scans every column, then every row, for a line that has no puppy yet and
        /// exactly one cell still available (empty, not X-marked, not error-locked).
        /// The cell must also be a legal placement, otherwise the line is a player
        /// mistake rather than a deduction and gets skipped.
        /// </summary>
        private bool TryFindForcedLineCell(GameManager gm, int w, int h, out Vector2Int cell, out string message)
        {
            for (int x = 0; x < w; x++)
            {
                if (TryGetSingleAvailableInLine(gm, x, x, 0, h - 1, out cell) && gm.IsLegalPlacement(cell))
                {
                    message = string.Format(hintOnlyCellInColumnMessage, x + 1);
                    return true;
                }
            }

            for (int y = 0; y < h; y++)
            {
                if (TryGetSingleAvailableInLine(gm, 0, w - 1, y, y, out cell) && gm.IsLegalPlacement(cell))
                {
                    message = string.Format(hintOnlyCellInRowMessage, y + 1);
                    return true;
                }
            }

            cell = default;
            message = null;
            return false;
        }

        private static bool TryGetSingleAvailableInLine(GameManager gm, int xMin, int xMax, int yMin, int yMax,
                                                        out Vector2Int single)
        {
            single = default;
            int available = 0;

            for (int x = xMin; x <= xMax; x++)
            {
                for (int y = yMin; y <= yMax; y++)
                {
                    Cell c = gm.Grid.GetCell(x, y);
                    if (c == null) return false;
                    if (c.GetPuppy() != null) return false;         // line already satisfied
                    if (!c.CanReceiveHint || c.IsXMarked) continue; // excluded / error-locked

                    available++;
                    if (available > 1) return false;
                    single = new Vector2Int(x, y);
                }
            }

            return available == 1;
        }

        // ---- Execution (board dim/preview + popup) ----

        private void ExecuteHint(GameManager gm, HintPlan plan, HintPopup popup)
        {
            _hintMode = plan.Mode;
            _hintActive = true;
            GameManager.InputLocked = true;

            DimAllExcept(gm, gm.GridWidth, gm.GridHeight, pos => plan.BrightCells.Contains(pos));

            _focusCell = plan.FocusCell;
            if (_focusCell != null) _focusCell.ShowHintFocusGlow();

            _previewCells.Clear();
            if (plan.PreviewCells != null) _previewCells.AddRange(plan.PreviewCells);
            _placementTarget = plan.PlacementTarget;

            popup.Setup(plan.Message, ApplyHint, OnHintPopupDismissed);

            if (_previewCells.Count > 0)
                _hintRoutine = StartCoroutine(StaggeredPreview());
        }

        private IEnumerator StaggeredPreview()
        {
            var wait = new WaitForSeconds(hintStagger);
            // Iterate a snapshot: ExitHint (via dismiss) can clear _previewCells mid-stagger,
            // which would otherwise throw "Collection was modified" on the next resume.
            Cell[] cells = _previewCells.ToArray();
            foreach (Cell cell in cells)
            {
                if (cell == null) continue;
                cell.ShowXPreview();
                if (hintStagger > 0f) yield return wait;
            }
            _hintRoutine = null;
        }

        // ---- Popup callbacks ----

        /// <summary>Apply pressed on the hint popup: commit the previewed X's, or place
        /// the puppy on the highlighted cell. The popup closes itself.</summary>
        private void ApplyHint()
        {
            if (!_hintActive) return;

            if (_hintMode == HintMode.Placement)
            {
                if (GameManager.Instance != null)
                    GameManager.Instance.PlacePuppyAtIfValid(_placementTarget);
                ExitHint();
                return;
            }

            _hintRoutine = StartCoroutine(ApplyHintRoutine());
        }

        private IEnumerator ApplyHintRoutine()
        {
            var wait = new WaitForSeconds(hintStagger);
            Cell[] cells = _previewCells.ToArray();
            foreach (Cell cell in cells)
            {
                if (cell == null) continue;
                cell.CommitXPreview();
                if (hintStagger > 0f) yield return wait;
            }
            ExitHint();
        }

        /// <summary>Popup closed without Apply (X button / preempted): clear previews
        /// and refund the X-preview steps; placement steps keep the cost.</summary>
        private void OnHintPopupDismissed()
        {
            if (!_hintActive) return;

            if (_hintMode == HintMode.Elimination || _hintMode == HintMode.Invalidation)
            {
                foreach (Cell cell in _previewCells)
                    if (cell != null) cell.ClearXPreview();

                // Backing out of the eliminations/invalidation shouldn't cost the player.
                EconomyHandler.GrantBooster(BoosterType.Hint);
            }

            ExitHint();
        }

        // ---- Hint teardown ----

        private void DimAllExcept(GameManager gm, int w, int h, Func<Vector2Int, bool> keepBright)
        {
            _dimmedCells.Clear();
            for (int x = 0; x < w; x++)
            {
                for (int y = 0; y < h; y++)
                {
                    var pos = new Vector2Int(x, y);
                    if (keepBright(pos)) continue;

                    Cell cell = gm.Grid.GetCell(x, y);
                    if (cell == null) continue;
                    cell.SetDimmed(true);
                    _dimmedCells.Add(cell);
                }
            }
        }

        private void ExitHint()
        {
            if (!_hintActive) return;
            _hintActive = false;

            if (_hintRoutine != null)
            {
                StopCoroutine(_hintRoutine);
                _hintRoutine = null;
            }

            foreach (Cell cell in _dimmedCells)
                if (cell != null) cell.SetDimmed(false);
            _dimmedCells.Clear();

            if (_focusCell != null) _focusCell.ClearHintFocusGlow();
            _focusCell = null;
            _previewCells.Clear();

            GameManager.InputLocked = false;

            // Close the popup if it is still open (teardown paths — Apply/dismiss
            // already close it; CloseCurrent no-ops while a close is in progress).
            if (PopupCanvasManager.Instance != null && PopupCanvasManager.Instance.IsOpen(PopupType.Hint))
                PopupCanvasManager.Instance.Hide(PopupType.Hint);
        }
    }
}
