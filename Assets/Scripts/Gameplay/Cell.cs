using System;
using DG.Tweening;
using UnityEngine;

public class Cell : MonoBehaviour
{
    public static event Action<Vector2Int> OnCellDoubleTapped;

    [Header("Visual Elements")]
    [SerializeField] private SpriteRenderer zoneOverlay;
    [SerializeField] private SpriteRenderer whiteOverlay;
    [SerializeField] private SpriteRenderer line1;
    [SerializeField] private SpriteRenderer line2;
    [SerializeField] private Transform shadowTransform;   // optional, may be null
    [Tooltip("One-shot heart-break effect played during invalid-placement feedback. Optional.")]
    [SerializeField] private HeartUI heartBreakEffect;

    [Header("Cell Scale (prefab default = 0.3)")]
    [Tooltip("Idle scale of the cell root. Should match the prefab.")]
    [SerializeField] private Vector3 restingScale = new Vector3(0.3f, 0.3f, 0.3f);
    [Tooltip("Uniform scale at the bottom of the tap-press compression.")]
    [SerializeField] private float tapCompressScale = 0.26f;
    [Tooltip("Uniform scale at the top of the rebound overshoot before settling.")]
    [SerializeField] private float tapReboundScale = 0.34f;

    [Header("Tap Animation Timings")]
    [SerializeField] private float compressDuration = 0.06f;
    [SerializeField] private float reboundDuration = 0.10f;
    [SerializeField] private float overlayExpandDuration = 0.18f;
    [SerializeField] private float overlayFadeDuration = 0.15f;
    [Tooltip("Time for a single X-mark line to grow from 0 to its authored X scale.")]
    [SerializeField] private float lineDrawDuration = 0.28f;
    [Tooltip("Gap between line1 finishing its draw and line2 starting.")]
    [SerializeField] private float lineDelay = 0.08f;
    [Tooltip("Ease for the gradual X-mark line draw. OutCubic = smooth grow, OutQuad = even smoother, OutBack = pop at end.")]
    [SerializeField] private Ease lineDrawEase = Ease.OutCubic;

    [Header("White Overlay Pulse (multipliers of authored scale, e.g. 2.5)")]
    [SerializeField] private float overlayHiddenMul = 0.8f;
    [SerializeField] private float overlayPeakMul = 1.15f;
    [SerializeField] private float overlayFinalMul = 1.25f;
    [Range(0f, 1f)]
    [SerializeField] private float overlayPeakAlpha = 0.2f;

    [Header("Puppy Placement Pop")]
    [SerializeField] private float puppyAppearOvershoot = 1.1f;
    [SerializeField] private float puppyAppearDuration = 0.15f;
    [SerializeField] private float puppySettleDuration = 0.1f;

    [Header("Error Feedback — strictly sequential")]
    [SerializeField] private Color errorColor = new Color32(255, 73, 0, 255); // #FF4900

    [Tooltip("Step 1 — Duration of the WHITE overlay flash (expand + fade).")]
    [SerializeField] private float whiteFlashDuration = 0.35f;
    [Range(0f, 1f)]
    [SerializeField] private float whiteFlashAlpha = 0.45f;
    [Tooltip("Scale multiplier on the overlay's authored scale during the flash.")]
    [SerializeField] private float whiteFlashExpandMul = 1.2f;

    [Tooltip("Step 2 — Cell shake duration.")]
    [SerializeField] private float shakeDuration = 0.25f;
    [SerializeField] private float shakeStrength = 0.05f;

    [Tooltip("Step 3 — Time for the zone overlay to flip to error red.")]
    [SerializeField] private float zoneTintInDuration = 0.15f;

    [Tooltip("Step 4 — Duration of the WHITE X line draw with overshoot.")]
    [SerializeField] private float whiteXDrawDuration = 0.25f;
    [Tooltip("Brief hold of the white X on screen before it retracts.")]
    [SerializeField] private float whiteXHoldDuration = 0.10f;
    [Tooltip("Step 5 — Duration of the WHITE X retract (shrink to 0).")]
    [SerializeField] private float whiteXHideDuration = 0.15f;

    [Tooltip("Step 7 — Time for the zone overlay to fade back to its original color.")]
    [SerializeField] private float zoneTintRestoreDuration = 0.50f;

    [Tooltip("Step 8 — Duration of the red X line draw with overshoot. Stays on screen permanently.")]
    [SerializeField] private float redLineDrawDuration = 0.40f;

    public Vector2Int gridPosition;
    public int zoneID;
    public bool IsXMarked;
    public bool IsErrorLocked;
    public bool isGiven;

    /// <summary>Zone color overlay child renderer, exposed for the grid intro animation.</summary>
    public SpriteRenderer ZoneOverlay => zoneOverlay;

    private PuzzleObject currentPuppy;
    private Color originalZoneColor;
    private bool isAnimating;

    // Authored scales captured at Init (overlay 2.5, lines 0.5, etc.).
    private Vector3 whiteOverlayBaseScale;
    private Vector3 line1BaseScale;
    private Vector3 line2BaseScale;

    public void Init(int x, int y, int zone, Color zoneColor)
    {
        gridPosition = new Vector2Int(x, y);
        zoneID = zone;
        originalZoneColor = zoneColor;
        isGiven = false;
        IsErrorLocked = false;
        IsXMarked = false;

        transform.localScale = restingScale;

        if (zoneOverlay != null)
            zoneOverlay.color = zoneColor;

        if (whiteOverlay != null)
        {
            whiteOverlayBaseScale = whiteOverlay.transform.localScale; // e.g. (2.5, 2.5, 2.5)
            whiteOverlay.gameObject.SetActive(true);
            Color c = whiteOverlay.color;
            c.a = 0f;
            whiteOverlay.color = c;
            whiteOverlay.transform.localScale = whiteOverlayBaseScale * overlayHiddenMul;
        }
        else
        {
            Debug.LogWarning($"Cell ({x},{y}): whiteOverlay missing.");
        }

        if (line1 != null)
        {
            line1BaseScale = line1.transform.localScale;              // e.g. (0.5, 0.5, 0.5)
            line1.gameObject.SetActive(false);
        }
        if (line2 != null)
        {
            line2BaseScale = line2.transform.localScale;
            line2.gameObject.SetActive(false);
        }

        gameObject.name = $"Cell_{x}_{y}_Zone{zone}";
    }

    // ---- Single tap: toggle white X ----
    public void ToggleXMark()
    {
        if (currentPuppy != null || IsErrorLocked) return;

        if (IsXMarked)
            HideXMarkWithAnimation();
        else
            ShowXMarkWithAnimation();
    }

    private void ShowXMarkWithAnimation()
    {
        if (isAnimating) return;
        isAnimating = true;
        IsXMarked = true;

        KillAllTweens();

        // 1. Cell compression → rebound → settle.
        Sequence cellSeq = DOTween.Sequence();
        cellSeq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        cellSeq.Append(transform.DOScale(Vector3.one * tapCompressScale, compressDuration).SetEase(Ease.OutQuad))
               .Append(transform.DOScale(Vector3.one * tapReboundScale, reboundDuration).SetEase(Ease.OutBack))
               .Append(transform.DOScale(restingScale, reboundDuration * 0.5f).SetEase(Ease.OutCubic));

        // 2. White overlay flash (expand around the cell, then fade out larger).
        if (whiteOverlay != null)
        {
            whiteOverlay.gameObject.SetActive(true);
            whiteOverlay.transform.localScale = whiteOverlayBaseScale * overlayHiddenMul;
            Color cStart = whiteOverlay.color; cStart.a = 0f; whiteOverlay.color = cStart;

            Sequence overlaySeq = DOTween.Sequence();
            overlaySeq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
            overlaySeq
                .Append(whiteOverlay.DOFade(overlayPeakAlpha, overlayExpandDuration * 0.5f))
                .Join(whiteOverlay.transform.DOScale(whiteOverlayBaseScale * overlayPeakMul, overlayExpandDuration).SetEase(Ease.OutQuad))
                .Append(whiteOverlay.DOFade(0f, overlayFadeDuration))
                .Join(whiteOverlay.transform.DOScale(whiteOverlayBaseScale * overlayFinalMul, overlayFadeDuration));
        }

        // 3. Optional shadow press.
        if (shadowTransform != null)
            shadowTransform.DOPunchPosition(Vector3.down * 0.05f, 0.2f, 5, 0.5f).SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        // 4. Draw the X lines (preserve the authored Y/Z scale, only animate X from 0 → base.x).
        Sequence xSeq = DOTween.Sequence();
        xSeq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        xSeq.AppendInterval(0.05f);
        // Draw both lines at the same time.
        if (line1 != null)
        {
            line1.gameObject.SetActive(true);
            line1.color = Color.white;
            line1.transform.localScale = new Vector3(0f, line1BaseScale.y, line1BaseScale.z);
            xSeq.Append(line1.transform.DOScaleX(line1BaseScale.x, lineDrawDuration).SetEase(lineDrawEase));
        }
        if (line2 != null)
        {
            line2.gameObject.SetActive(true);
            line2.color = Color.white;
            line2.transform.localScale = new Vector3(0f, line2BaseScale.y, line2BaseScale.z);
            xSeq.Join(line2.transform.DOScaleX(line2BaseScale.x, lineDrawDuration).SetEase(lineDrawEase));
        }
        xSeq.OnComplete(() => isAnimating = false);
    }

    private void HideXMarkWithAnimation()
    {
        if (isAnimating) return;
        isAnimating = true;
        IsXMarked = false;

        KillAllTweens();

        Sequence hideSeq = DOTween.Sequence();
        hideSeq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        if (line1 != null) hideSeq.Join(line1.transform.DOScaleX(0f, 0.08f).SetEase(Ease.InQuad));
        if (line2 != null) hideSeq.Join(line2.transform.DOScaleX(0f, 0.08f).SetEase(Ease.InQuad));
        hideSeq.Join(transform.DOScale(restingScale * 0.9f, 0.05f).SetLoops(2, LoopType.Yoyo));
        hideSeq.OnComplete(() =>
        {
            if (line1 != null) line1.gameObject.SetActive(false);
            if (line2 != null) line2.gameObject.SetActive(false);
            transform.localScale = restingScale;
            isAnimating = false;
        });
    }

    // ---- Permanent red X (invalid move) ----
    /// <summary>
    /// Strictly sequential error feedback. Each step fully completes before the
    /// next one starts: white flash → shake → zone→red → white X draws → white
    /// X disappears → heart break → zone fades back → red X draws. Cell stays
    /// locked afterwards.
    /// </summary>
    public void ShowPermanentRedCross()
    {
        if (currentPuppy != null) return;

        IsErrorLocked = true;
        IsXMarked = false;
        KillAllTweens();
        isAnimating = true;

        // Pre-stage X lines hidden at width-0; coloured WHITE for step 4.
        if (line1 != null)
        {
            line1.gameObject.SetActive(true);
            line1.color = Color.white;
            line1.transform.localScale = new Vector3(0f, line1BaseScale.y, line1BaseScale.z);
        }
        if (line2 != null)
        {
            line2.gameObject.SetActive(true);
            line2.color = Color.white;
            line2.transform.localScale = new Vector3(0f, line2BaseScale.y, line2BaseScale.z);
        }

        Sequence errSeq = DOTween.Sequence();
        errSeq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        // Step 1 — WHITE overlay flash (expand + fade).
        if (whiteOverlay != null)
        {
            whiteOverlay.gameObject.SetActive(true);
            Color c = Color.white; c.a = whiteFlashAlpha;
            whiteOverlay.color = c;
            whiteOverlay.transform.localScale = whiteOverlayBaseScale;

            Sequence flashSeq = DOTween.Sequence();
            flashSeq.Join(whiteOverlay.transform.DOScale(whiteOverlayBaseScale * whiteFlashExpandMul, whiteFlashDuration).SetEase(Ease.OutQuad));
            flashSeq.Join(whiteOverlay.DOFade(0f, whiteFlashDuration).SetEase(Ease.OutQuad));
            errSeq.Append(flashSeq);
        }
        else
        {
            errSeq.AppendInterval(whiteFlashDuration);
        }

        // Step 2 — cell shake.
        errSeq.Append(transform.DOShakePosition(shakeDuration, shakeStrength, 10, 90, false, true));

        // Step 3 — zone overlay flips to error red.
        if (zoneOverlay != null)
            errSeq.Append(zoneOverlay.DOColor(errorColor, zoneTintInDuration).SetEase(Ease.OutQuad));
        else
            errSeq.AppendInterval(zoneTintInDuration);

        // Step 4 — WHITE X draws with overshoot (both lines together).
        Sequence whiteDrawSeq = DOTween.Sequence();
        if (line1 != null)
            whiteDrawSeq.Join(line1.transform.DOScaleX(line1BaseScale.x, whiteXDrawDuration).SetEase(Ease.OutBack));
        if (line2 != null)
            whiteDrawSeq.Join(line2.transform.DOScaleX(line2BaseScale.x, whiteXDrawDuration).SetEase(Ease.OutBack));
        errSeq.Append(whiteDrawSeq);

        // Brief hold so the white X reads, then step 5 — WHITE X disappears.
        errSeq.AppendInterval(whiteXHoldDuration);
        Sequence whiteHideSeq = DOTween.Sequence();
        if (line1 != null)
            whiteHideSeq.Join(line1.transform.DOScaleX(0f, whiteXHideDuration).SetEase(Ease.InQuad));
        if (line2 != null)
            whiteHideSeq.Join(line2.transform.DOScaleX(0f, whiteXHideDuration).SetEase(Ease.InQuad));
        errSeq.Append(whiteHideSeq);

        // Step 6 — heart break plays, sequence waits for its known duration.
        // Null-check inside the lambda too: ~1.25s after we get here, the
        // effect could have been destroyed (level reload, scene change).
        if (heartBreakEffect != null)
        {
            errSeq.AppendCallback(() =>
            {
                if (heartBreakEffect != null) heartBreakEffect.PlayBreak();
            });
            errSeq.AppendInterval(heartBreakEffect.BreakDuration);
        }

        // Step 7 — zone fades back to its original color.
        if (zoneOverlay != null)
            errSeq.Append(zoneOverlay.DOColor(originalZoneColor, zoneTintRestoreDuration).SetEase(Ease.OutCubic));
        else
            errSeq.AppendInterval(zoneTintRestoreDuration);

        // Step 8 — RED X draws with overshoot, permanent.
        errSeq.AppendCallback(() =>
        {
            if (line1 != null) line1.color = errorColor;
            if (line2 != null) line2.color = errorColor;
        });
        Sequence redDrawSeq = DOTween.Sequence();
        if (line1 != null)
            redDrawSeq.Join(line1.transform.DOScaleX(line1BaseScale.x, redLineDrawDuration).SetEase(Ease.OutBack));
        if (line2 != null)
            redDrawSeq.Join(line2.transform.DOScaleX(line2BaseScale.x, redLineDrawDuration).SetEase(Ease.OutBack));
        errSeq.Append(redDrawSeq);

        errSeq.OnComplete(() => isAnimating = false);
    }

    // ---- Puppy placement (called by GameManager) ----
    public PuzzleObject PlacePuppy(GameObject puppyPrefab)
    {
        if (currentPuppy != null)
        {
            Debug.LogWarning($"Cell {gridPosition}: already has a puppy!");
            return null;
        }

        // Clear any X / overlay state.
        IsXMarked = false;
        IsErrorLocked = false;
        KillAllTweens();
        if (line1 != null) line1.gameObject.SetActive(false);
        if (line2 != null) line2.gameObject.SetActive(false);
        if (whiteOverlay != null)
        {
            Color c = whiteOverlay.color; c.a = 0f; whiteOverlay.color = c;
        }

        Vector3 worldPos = transform.position;
        worldPos.z = -0.1f;

        GameObject pupObj = Instantiate(puppyPrefab, worldPos, Quaternion.identity, transform);
        PuzzleObject pup = pupObj.GetComponent<PuzzleObject>();
        if (pup == null)
        {
            Debug.LogError("Prefab is missing PuzzleObject script!");
            Destroy(pupObj);
            return null;
        }

        pup.Init(gridPosition);
        currentPuppy = pup;

        // Dim the zone color to show occupation.
        if (zoneOverlay != null)
            zoneOverlay.color = Color.Lerp(originalZoneColor, Color.gray, 0.5f);

        // Pop-in animation, scaled around the puppy's authored prefab scale.
        // Link to the PUPPY object (not the cell) — if the puppy is destroyed
        // via RemovePuppy() while the pop is still running, the tween dies too
        // and can't crash trying to write to a destroyed transform.
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

    public void RemovePuppy()
    {
        if (currentPuppy == null) return;
        Destroy(currentPuppy.gameObject);
        currentPuppy = null;
        if (zoneOverlay != null) zoneOverlay.color = originalZoneColor;
    }

    // ---- Double tap: request a placement attempt ----
    public void OnDoubleTap()
    {
        if (currentPuppy != null || IsErrorLocked) return;
        OnCellDoubleTapped?.Invoke(gridPosition);
    }

    public void AttemptPlacement() => OnDoubleTap();

    private void KillAllTweens()
    {
        transform.DOKill();
        if (whiteOverlay != null)
        {
            whiteOverlay.transform.DOKill();
            whiteOverlay.DOKill();
        }
        if (line1 != null) line1.transform.DOKill();
        if (line2 != null) line2.transform.DOKill();
        if (shadowTransform != null) shadowTransform.DOKill();
    }

    private void OnDestroy()
    {
        KillAllTweens();
    }
}
