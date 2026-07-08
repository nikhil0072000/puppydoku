using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// One grid cell. Owns the per-cell visuals for the new prefab hierarchy:
///
///   BG             — always-visible backdrop, tinted with the zone colour. Also pulsed
///                    as the Bulb-hint focus glow and dimmed during hints.
///   Visual         — inner face; placed puppies (PuzzleObject) parent here.
///   CrossContainer — inactive by default; the player's X note mark, toggled by single
///                    tap / swipe. Its Animator auto-plays the cross-draw animation on
///                    activation (replaces the old DOTween line draw). Also shown as the
///                    permanent cross on a wrong move and as the hint ghost-X preview.
///   HeartContainer — inactive by default; HeartUI heart-break effect on a wrong move.
/// </summary>
public class Cell : MonoBehaviour
{
    public static event Action<Vector2Int> OnCellDoubleTapped;

    [Header("Visual References")]
    [Tooltip("Backdrop sprite — tinted with the zone colour. Also pulsed as the hint focus glow.")]
    [SerializeField] private SpriteRenderer bg;
    [Tooltip("Inner cell face. Placed puppies are parented here.")]
    [SerializeField] private SpriteRenderer visual;
    [Tooltip("Inactive player X mark, toggled by single tap / swipe. Its Animator plays the cross-draw animation on activation.")]
    [SerializeField] private GameObject crossContainer;
    [Tooltip("One-shot heart-break effect played on a wrong move (double tap on an invalid cell).")]
    [SerializeField] private HeartUI heartBreakEffect;

    [Header("Cell Scale (prefab default = 0.3)")]
    [Tooltip("Idle scale of the cell root. Should match the prefab.")]
    [SerializeField] private Vector3 restingScale = new Vector3(0.3f, 0.3f, 0.3f);
    /// <summary>Idle scale of the cell root (read-only, exposed for GridManager's auto-size detection).</summary>
    public Vector3 RestingScale => restingScale;
    private Vector3 runtimeRestingScale;
    private Vector3 CurrentRestingScale => runtimeRestingScale == Vector3.zero ? restingScale : runtimeRestingScale;

    [Tooltip("Uniform scale at the bottom of the tap-press compression.")]
    [SerializeField] private float tapCompressScale = 0.26f;
    [Tooltip("Uniform scale at the top of the rebound overshoot before settling.")]
    [SerializeField] private float tapReboundScale = 0.34f;

    [Header("Tap Animation Timings")]
    [SerializeField] private float compressDuration = 0.06f;
    [SerializeField] private float reboundDuration = 0.10f;

    [Header("Puppy Placement Pop")]
    [SerializeField] private float puppyAppearOvershoot = 1.1f;
    [SerializeField] private float puppyAppearDuration = 0.15f;
    [SerializeField] private float puppySettleDuration = 0.1f;

    [Header("Hint Power-Up")]
    [Tooltip("Alpha of the ghost (preview) X before the player presses Apply.")]
    [Range(0f, 1f)]
    [SerializeField] private float xPreviewAlpha = 0.4f;
    [Tooltip("Fallback colour of the focus-cell glow when the theme provides none.")]
    [SerializeField] private Color hintFocusColor = new Color32(255, 200, 60, 255); // warm gold
    [Tooltip("How dark non-relevant cells get dimmed during a hint (0 = unchanged, 1 = black).")]
    [Range(0f, 1f)]
    [SerializeField] private float hintDimAmount = 0.6f;

    public Vector2Int gridPosition;
    public int zoneID;
    public bool IsXMarked;
    public bool IsErrorLocked;
    public bool isGiven;

    /// <summary>Zone-coloured backdrop renderer, exposed for the grid intro animation's fade-in.</summary>
    public SpriteRenderer Bg => bg;

    private PuzzleObject currentPuppy;
    private Color originalZoneColor;

    // Focus-glow state: the bg colour to restore when the glow clears. Captured at
    // glow start so an occupied cell's gray tint survives the hint (restoring the
    // raw zone colour would make the cell look empty again).
    private bool isGlowing;
    private Color preGlowBgColor;

    // Renderers under the CrossContainer, captured at Init so the hint preview can fade them.
    private SpriteRenderer[] crossRenderers;

    // ---- Hint state ----
    private bool isXPreviewing;   // ghost X shown, not yet committed
    private bool isDimmed;
    private Color preDimZoneColor;
    private SpriteRenderer[] puppyRenderers; // captured when dimming, to dim the puppy too
    private Color[] puppyOriginalColors;     // their colours before dimming, to restore exactly

    public void Init(int x, int y, int zone, Color zoneColor)
    {
        gridPosition = new Vector2Int(x, y);
        zoneID = zone;
        originalZoneColor = zoneColor;
        isGiven = false;
        IsErrorLocked = false;
        IsXMarked = false;
        isXPreviewing = false;
        isDimmed = false;

        transform.localScale = restingScale;
        runtimeRestingScale = restingScale;

        ApplyTheme(ThemeManager.Current);

        if (bg != null)
            bg.color = zoneColor;
        isGlowing = false;

        if (crossContainer != null)
        {
            crossRenderers = crossContainer.GetComponentsInChildren<SpriteRenderer>(true);
            crossContainer.SetActive(false);
        }

        if (heartBreakEffect != null)
            heartBreakEffect.gameObject.SetActive(false);

        gameObject.name = $"Cell_{x}_{y}_Zone{zone}";
    }

    private Color GetHintGlowColor()
    {
        ThemeData theme = ThemeManager.Current;
        return theme != null ? theme.hintGlowColor : hintFocusColor;
    }

    public void SetRuntimeRestingScale(Vector3 scale)
    {
        runtimeRestingScale = scale;
    }

    // ---- Single tap / swipe: toggle the X note mark ----

    public void ToggleXMark()
    {
        if (currentPuppy != null || IsErrorLocked) return;

        IsXMarked = !IsXMarked;

        if (crossContainer != null)
        {
            SetCrossAlpha(1f);
            // Activation restarts the Animator's default cross-draw state from the top.
            crossContainer.SetActive(false);
            if (IsXMarked) crossContainer.SetActive(true);
        }

        PlayTapPunch();
    }

    /// <summary>Cell compression → rebound → settle, shared by mark and unmark.</summary>
    private void PlayTapPunch()
    {
        transform.DOKill();

        float compressFactor = (restingScale.x > 0f) ? tapCompressScale / restingScale.x : 1f;
        float reboundFactor = (restingScale.x > 0f) ? tapReboundScale / restingScale.x : 1f;

        Sequence cellSeq = DOTween.Sequence();
        cellSeq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        cellSeq.Append(transform.DOScale(CurrentRestingScale * compressFactor, compressDuration).SetEase(Ease.OutQuad))
               .Append(transform.DOScale(CurrentRestingScale * reboundFactor, reboundDuration).SetEase(Ease.OutBack))
               .Append(transform.DOScale(CurrentRestingScale, reboundDuration * 0.5f).SetEase(Ease.OutCubic));
    }

    // ---- Permanent red X (invalid move) ----

    /// <summary>
    /// Wrong-move feedback (double tap on an invalid cell): the heart-break effect
    /// plays first, then the permanent cross draws once it finishes. The cell locks
    /// immediately; any note X on it is cleared.
    /// </summary>
    public void ShowPermanentRedCross()
    {
        if (currentPuppy != null) return;

        IsErrorLocked = true;
        IsXMarked = false;

        // Clear any note X; the permanent cross draws after the heart-break finishes.
        if (crossContainer != null)
            crossContainer.SetActive(false);

        float crossDelay = 0f;
        if (heartBreakEffect != null)
        {
            heartBreakEffect.gameObject.SetActive(true);
            heartBreakEffect.PlayBreak();
            crossDelay = heartBreakEffect.BreakDuration;
        }

        DOTween.Sequence()
               .AppendInterval(crossDelay)
               .AppendCallback(() =>
               {
                   // The cell could have been reused (level reload) meanwhile.
                   if (crossContainer != null && IsErrorLocked)
                   {
                       SetCrossAlpha(1f);
                       crossContainer.SetActive(true);
                   }
               })
               .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    // ---- Puppy placement (called by GameManager) ----

    public PuzzleObject PlacePuppy(GameObject puppyPrefab)
    {
        if (currentPuppy != null)
        {
            Debug.LogWarning($"Cell {gridPosition}: already has a puppy!");
            return null;
        }

        // Clear any X / error state.
        IsXMarked = false;
        IsErrorLocked = false;
        isXPreviewing = false;
        if (crossContainer != null) crossContainer.SetActive(false);

        Vector3 worldPos = transform.position;
        worldPos.z = -0.1f;

        Transform puppyParent = visual != null ? visual.transform : transform;
        GameObject pupObj = Instantiate(puppyPrefab, worldPos, Quaternion.identity, puppyParent);
        PuzzleObject pup = pupObj.GetComponent<PuzzleObject>();
        if (pup == null)
        {
            Debug.LogError("Prefab is missing PuzzleObject script!");
            Destroy(pupObj);
            return null;
        }

        pup.Init(gridPosition);
        currentPuppy = pup;

        // Dim the zone colour to show occupation.
        if (bg != null)
            bg.color = Color.Lerp(originalZoneColor, Color.gray, 0.5f);

        // Pop-in animation, scaled around the puppy's authored prefab scale.
        // Link to the PUPPY object — if it is destroyed while the pop is still
        // running, the tween dies with it.
        Vector3 puppyFinal = pupObj.transform.localScale;
        Vector3 puppyPeak = puppyFinal * puppyAppearOvershoot;
        pupObj.transform.localScale = Vector3.zero;
        Sequence puppySeq = DOTween.Sequence();
        puppySeq.SetLink(pupObj, LinkBehaviour.KillOnDestroy);
        puppySeq.Append(pupObj.transform.DOScale(puppyPeak, puppyAppearDuration).SetEase(Ease.OutBack))
                .Append(pupObj.transform.DOScale(puppyFinal, puppySettleDuration).SetEase(Ease.OutCubic));

        return pup;
    }

    public PuzzleObject GetPuppy() => currentPuppy;

    // ---- Double tap: request a placement attempt ----

    public void OnDoubleTap()
    {
        if (currentPuppy != null || IsErrorLocked) return;
        OnCellDoubleTapped?.Invoke(gridPosition);
    }

    // ---- Theme application (visual-only) ----

    /// <summary>Applies theme-driven sprites. Called once during Init. Does NOT touch gameplay state.</summary>
    private void ApplyTheme(ThemeData theme)
    {
        if (theme == null) return;

        if (visual != null && theme.cellZoneOverlaySprite != null)
            visual.sprite = theme.cellZoneOverlaySprite;

        if (bg != null && theme.cellBackgroundSprite != null)
            bg.sprite = theme.cellBackgroundSprite;
    }

    // ---- Hint power-up (Bulb) ----

    /// <summary>True when this cell can receive a hint preview X (empty, not given, not error-locked).</summary>
    public bool CanReceiveHint => currentPuppy == null && !isGiven && !IsErrorLocked;

    /// <summary>Pulsing glow on the BG backdrop marking the puppy the hint is reasoning from.</summary>
    public void ShowHintFocusGlow()
    {
        if (bg == null) return;

        bg.DOKill();
        if (!isGlowing)
        {
            isGlowing = true;
            preGlowBgColor = bg.color;
        }
        bg.color = GetHintGlowColor();

        bg.DOFade(0.45f, 0.7f)
          .SetLoops(-1, LoopType.Yoyo)
          .SetEase(Ease.InOutSine)
          .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    public void ClearHintFocusGlow()
    {
        if (bg == null || !isGlowing) return;

        isGlowing = false;
        bg.DOKill();
        bg.color = preGlowBgColor;
    }

    /// <summary>Dims (or restores) this cell — including any puppy on it — to push it
    /// into the background while the hint focuses elsewhere.</summary>
    public void SetDimmed(bool dim)
    {
        if (dim == isDimmed) return;
        isDimmed = dim;

        if (dim)
        {
            if (bg != null)
            {
                preDimZoneColor = bg.color;
                bg.DOColor(Color.Lerp(preDimZoneColor, Color.black, hintDimAmount), 0.2f)
                  .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }
            if (currentPuppy != null)
            {
                puppyRenderers = currentPuppy.GetComponentsInChildren<SpriteRenderer>();
                puppyOriginalColors = new Color[puppyRenderers.Length];
                for (int i = 0; i < puppyRenderers.Length; i++)
                {
                    puppyOriginalColors[i] = puppyRenderers[i].color;
                    puppyRenderers[i].DOColor(Color.Lerp(puppyOriginalColors[i], Color.black, hintDimAmount), 0.2f)
                                     .SetLink(puppyRenderers[i].gameObject, LinkBehaviour.KillOnDestroy);
                }
            }
        }
        else
        {
            if (bg != null)
                bg.DOColor(preDimZoneColor, 0.2f).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            if (puppyRenderers != null)
            {
                for (int i = 0; i < puppyRenderers.Length; i++)
                    if (puppyRenderers[i] != null)
                        puppyRenderers[i].DOColor(puppyOriginalColors[i], 0.2f)
                                         .SetLink(puppyRenderers[i].gameObject, LinkBehaviour.KillOnDestroy);
                puppyRenderers = null;
                puppyOriginalColors = null;
            }
        }
    }

    /// <summary>Shows a semi-transparent ghost X (suggested mark) without committing it.</summary>
    public void ShowXPreview()
    {
        if (!CanReceiveHint || isXPreviewing || IsXMarked) return;
        isXPreviewing = true;

        if (crossContainer != null)
        {
            SetCrossAlpha(xPreviewAlpha);
            crossContainer.SetActive(true);
        }
    }

    /// <summary>Turns the ghost X into a permanent X (equivalent to a player tap).</summary>
    public void CommitXPreview()
    {
        if (!isXPreviewing) return;
        isXPreviewing = false;
        IsXMarked = true;

        if (crossRenderers != null)
        {
            foreach (SpriteRenderer sr in crossRenderers)
            {
                if (sr == null) continue;
                sr.DOKill();
                sr.DOFade(1f, 0.2f).SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            }
        }
    }

    /// <summary>Removes the ghost X if it was never applied.</summary>
    public void ClearXPreview()
    {
        if (!isXPreviewing) return;
        isXPreviewing = false;

        if (crossContainer != null)
        {
            SetCrossAlpha(1f);
            crossContainer.SetActive(false);
        }
    }

    private void SetCrossAlpha(float alpha)
    {
        if (crossRenderers == null) return;
        foreach (SpriteRenderer sr in crossRenderers)
        {
            if (sr == null) continue;
            sr.DOKill();
            Color c = sr.color;
            c.a = alpha;
            sr.color = c;
        }
    }

    private void OnDestroy()
    {
        transform.DOKill();
        if (bg != null) bg.DOKill();
        if (visual != null) visual.DOKill();
        if (crossRenderers != null)
            foreach (SpriteRenderer sr in crossRenderers)
                if (sr != null) sr.DOKill();
    }
}
