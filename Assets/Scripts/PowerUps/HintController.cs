using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace PuppyPuzzle.PowerUps
{
    /// <summary>
    /// Bulb power-up: a progressive guided-deduction hint. Each press advances:
    ///
    ///  1. ELIMINATION — finds the most-recently-revealed puppy that still has
    ///     UNMARKED forbidden cells (its row, column, diagonal touches) and previews
    ///     X marks only on those still-unmarked cells. Apply commits them; Cancel
    ///     discards and refunds the chance. Pressing again walks back through older
    ///     puppies (recent → previous → initial/given).
    ///  2. DEDUCTION — once every revealed puppy's forbidden cells are already marked
    ///     (or no puppy is revealed yet), it outlines the cell where a puppy must go
    ///     (from the solver) in gold and shows guidance text. Dismiss to close.
    ///
    /// A chance is consumed up front by PowerUpButton; <see cref="TryActivate"/>
    /// returns false (refunding it) only when there is genuinely nothing left to show.
    /// </summary>
    public class HintController : PowerUpBase
    {
        private enum HintMode { Elimination, Deduction, Invalidation }

        [Header("UI")]
        [Tooltip("Root of the hint overlay (dim backdrop + Apply/Cancel + tutorial text). Toggled on during the hint.")]
        [SerializeField] private GameObject hintOverlay;
        [SerializeField] private Button applyButton;
        [SerializeField] private Button cancelButton;
        [SerializeField] private TMP_Text tutorialText;

        [Header("Content")]
        [TextArea]
        [Tooltip("Shown while previewing X marks around a puppy (elimination step).")]
        [SerializeField] private string eliminationMessage =
            "This puppy's row, column and diagonals can't hold another puppy. Apply to mark them with an X.";
        [TextArea]
        [Tooltip("Shown when everything is marked and the hint points to where a puppy goes (deduction step).")]
        [SerializeField] private string deductionMessage =
            "By elimination, a puppy must go in the highlighted cell. Apply to place it there.";

        [TextArea]
        [Tooltip("Shown when no puppies are placed: mark every cell that is not part of the hidden solution.")]
        [SerializeField] private string invalidationMessage =
            "These cells are not part of the hidden solution. Apply to mark them with X's.";

        [Header("Timing")]
        [Tooltip("Stagger between each ghost-X / commit animation for a cascading feel.")]
        [Min(0f)]
        [SerializeField] private float stagger = 0.04f;

        public override PowerUpType Type => PowerUpType.Hint;

        // Cells touched this hint, so we can restore them on exit.
        private readonly List<Cell> _dimmedCells = new();
        private readonly List<Cell> _previewCells = new();
        private Cell _focusCell;
        private bool _isActive;
        private HintMode _mode;
        private Vector2Int _deductionTarget; // cell to place a puppy on when Apply is pressed in deduction mode

        private void Awake()
        {
            if (applyButton != null) applyButton.onClick.AddListener(ApplyHint);
            if (cancelButton != null) cancelButton.onClick.AddListener(CancelHint);
            if (hintOverlay != null) hintOverlay.SetActive(false);
        }

        private void OnDestroy()
        {
            if (applyButton != null) applyButton.onClick.RemoveListener(ApplyHint);
            if (cancelButton != null) cancelButton.onClick.RemoveListener(CancelHint);
        }

        public override bool TryActivate()
        {
            if (_isActive) return false; // a hint is already on screen

            GameManager gm = GameManager.Instance;
            if (gm == null || gm.Grid == null) return false;

            int w = gm.GridWidth;
            int h = gm.GridHeight;

            // 1) If the board is fresh and a hidden solution exists, mark all non-solution cells.
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
                    BeginInvalidation(gm, invalidCells, invalidSet, w, h);
                    return true;
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

                BeginElimination(gm, focus, puppyPos, new HashSet<Vector2Int>(restricted), unmarked, w, h);
                return true;
            }

            // 2) Everything derivable is marked (or no puppy yet) → point to where a puppy goes.
            if (gm.TryGetNextSolutionCell(out Vector2Int target))
            {
                BeginDeduction(gm, target, w, h);
                return true;
            }

            // 3) Nothing left to teach → let the caller refund the chance.
            return false;
        }

        // ---- Elimination step ----

        private void BeginElimination(GameManager gm, Cell focus, Vector2Int focusPos,
                                      HashSet<Vector2Int> band, List<Cell> unmarked, int w, int h)
        {
            _mode = HintMode.Elimination;
            _isActive = true;
            GameManager.InputLocked = true;

            DimAllExcept(gm, w, h, pos => pos == focusPos || band.Contains(pos));

            _focusCell = focus;
            focus.ShowHintFocusGlow();

            _previewCells.Clear();
            _previewCells.AddRange(unmarked);

            if (tutorialText != null) tutorialText.text = eliminationMessage;
            OpenOverlay(showApply: true);
            SetButtons(apply: true, cancel: true);

            StartCoroutine(StaggeredPreview());
        }

        private IEnumerator StaggeredPreview()
        {
            var wait = new WaitForSeconds(stagger);
            foreach (Cell cell in _previewCells)
            {
                cell.ShowXPreview();
                if (stagger > 0f) yield return wait;
            }
        }

        private void BeginInvalidation(GameManager gm, List<Cell> invalidCells, HashSet<Vector2Int> invalidSet, int w, int h)
        {
            _mode = HintMode.Invalidation;
            _isActive = true;
            GameManager.InputLocked = true;

            DimAllExcept(gm, w, h, pos => invalidSet.Contains(pos));

            _previewCells.Clear();
            _previewCells.AddRange(invalidCells);

            if (tutorialText != null) tutorialText.text = invalidationMessage;
            OpenOverlay(showApply: true);
            SetButtons(apply: true, cancel: true);

            StartCoroutine(StaggeredPreview());
        }

        // ---- Deduction step ----

        private void BeginDeduction(GameManager gm, Vector2Int target, int w, int h)
        {
            _mode = HintMode.Deduction;
            _isActive = true;
            _deductionTarget = target;
            GameManager.InputLocked = true;

            DimAllExcept(gm, w, h, pos => pos == target);

            _focusCell = gm.Grid.GetCell(target.x, target.y);
            if (_focusCell != null) _focusCell.ShowHintFocusGlow();

            _previewCells.Clear(); // no X previews — Apply places the puppy instead

            if (tutorialText != null) tutorialText.text = deductionMessage;
            OpenOverlay(showApply: true); // Apply = place the puppy on the highlighted cell
            SetButtons(apply: true, cancel: true);
        }

        // ---- Buttons ----

        public void ApplyHint()
        {
            if (!_isActive) return;

            if (_mode == HintMode.Deduction)
            {
                // Place the puppy on the highlighted cell, then close.
                if (GameManager.Instance != null)
                    GameManager.Instance.PlacePuppyAtIfValid(_deductionTarget);
                ExitHint();
                return;
            }

            StartCoroutine(ApplyRoutine());
        }

        private IEnumerator ApplyRoutine()
        {
            SetButtons(apply: false, cancel: false);
            var wait = new WaitForSeconds(stagger);
            foreach (Cell cell in _previewCells)
            {
                cell.CommitXPreview();
                if (stagger > 0f) yield return wait;
            }
            ExitHint();
        }

        public void CancelHint()
        {
            if (!_isActive) return;

            if (_mode == HintMode.Elimination || _mode == HintMode.Invalidation)
            {
                foreach (Cell cell in _previewCells)
                    cell.ClearXPreview();

                // Backing out of the eliminations/invalidation shouldn't cost the player.
                if (PowerUpManager.Instance != null)
                    PowerUpManager.Instance.GrantChance(PowerUpType.Hint);
            }
            // Deduction mode is informational — dismissing keeps the chance spent.

            ExitHint();
        }

        // ---- Shared ----

        private void DimAllExcept(GameManager gm, int w, int h, System.Func<Vector2Int, bool> keepBright)
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
            foreach (Cell cell in _dimmedCells)
                if (cell != null) cell.SetDimmed(false);
            _dimmedCells.Clear();

            if (_focusCell != null) _focusCell.ClearHintFocusGlow();
            _focusCell = null;
            _previewCells.Clear();

            if (hintOverlay != null) hintOverlay.SetActive(false);
            GameManager.InputLocked = false;
            _isActive = false;
        }

        private void OpenOverlay(bool showApply)
        {
            if (applyButton != null) applyButton.gameObject.SetActive(showApply);
            if (hintOverlay != null) hintOverlay.SetActive(true);
        }

        private void SetButtons(bool apply, bool cancel)
        {
            if (applyButton != null) applyButton.interactable = apply;
            if (cancelButton != null) cancelButton.interactable = cancel;
        }
    }
}
