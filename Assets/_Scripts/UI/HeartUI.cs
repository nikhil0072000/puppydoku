using DG.Tweening;
using UnityEngine;

/// <summary>
/// One-shot heart-break visual effect (world-space sprites). Plays on demand
/// for invalid-placement feedback at the cell: a full heart punches in, swaps
/// to broken halves, splits apart with rotation, fades out + shrinks, then
/// hides itself. Not a state indicator — the HUD lives counter is separate.
/// </summary>
public class HeartUI : MonoBehaviour
{
    [Header("References (SpriteRenderers)")]
    [SerializeField] private SpriteRenderer fullSprite;
    [SerializeField] private SpriteRenderer leftSprite;
    [SerializeField] private SpriteRenderer rightSprite;

    [Header("Punch")]
    [Tooltip("Peak scale multiplier during the impact punch.")]
    [SerializeField] private float punchScale = 1.2f;
    [Tooltip("Dip scale multiplier after the punch peak.")]
    [SerializeField] private float punchDip = 0.9f;
    [SerializeField] private float punchDuration = 0.08f;

    [Header("Split")]
    [Tooltip("How far each half moves apart, in local units.")]
    [SerializeField] private float splitDistance = 0.2f;
    [Tooltip("Rotation each half ends up at (Z degrees).")]
    [SerializeField] private float splitRotation = 15f;
    [SerializeField] private float splitDuration = 0.15f;

    [Header("Fade")]
    [SerializeField] private float fadeDuration = 0.2f;
    [Tooltip("Pieces shrink to this multiplier of their authored scale while fading.")]
    [SerializeField] private float fadeEndScale = 0.7f;

    /// <summary>Total duration of <see cref="PlayBreak"/>, used by sequential callers that need to wait for it.</summary>
    public float BreakDuration => punchDuration + splitDuration + fadeDuration;

    // Authored rest values captured once so animations always return cleanly,
    // regardless of which authored scale (e.g. 0.5) the pieces use.
    private Vector3 _leftRestPos;
    private Vector3 _rightRestPos;
    private Vector3 _leftRestScale;
    private Vector3 _rightRestScale;
    private Quaternion _leftRestRotation;
    private Quaternion _rightRestRotation;
    private Vector3 _fullRestScale;
    private bool _captured;

    private void Awake() => CaptureRestState();

    private void CaptureRestState()
    {
        if (_captured) return;
        if (leftSprite != null)
        {
            Transform t = leftSprite.transform;
            _leftRestPos = t.localPosition;
            _leftRestScale = t.localScale;
            _leftRestRotation = t.localRotation;
        }
        if (rightSprite != null)
        {
            Transform t = rightSprite.transform;
            _rightRestPos = t.localPosition;
            _rightRestScale = t.localScale;
            _rightRestRotation = t.localRotation;
        }
        if (fullSprite != null)
            _fullRestScale = fullSprite.transform.localScale;
        _captured = true;
    }

    /// <summary>
    /// Plays the full heart-break effect. Auto-hides everything when done.
    /// Safe to call repeatedly — any in-flight tween is killed first.
    /// </summary>
    public void PlayBreak()
    {
        CaptureRestState();   // safety: if invoked before Awake
        KillTweens();
        ResetPiecesToRest();

        // Start: only the full heart is visible at its resting state.
        if (fullSprite != null)
        {
            SetAlpha(fullSprite, 1f);
            fullSprite.gameObject.SetActive(true);
        }
        if (leftSprite != null)
        {
            SetAlpha(leftSprite, 1f);
            leftSprite.gameObject.SetActive(false);
        }
        if (rightSprite != null)
        {
            SetAlpha(rightSprite, 1f);
            rightSprite.gameObject.SetActive(false);
        }

        Sequence seq = DOTween.Sequence();
        // Linking to this gameObject kills the sequence (and its pending
        // callbacks, like the full→broken swap) if the heart is destroyed
        // mid-play. Without this, the swap callback could fire on already-
        // destroyed sprite refs.
        seq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        // 1. Impact punch on the full heart.
        if (fullSprite != null)
        {
            Transform t = fullSprite.transform;
            seq.Append(t.DOScale(_fullRestScale * punchScale, punchDuration * 0.5f).SetEase(Ease.OutQuad));
            seq.Append(t.DOScale(_fullRestScale * punchDip, punchDuration * 0.5f).SetEase(Ease.OutQuad));
        }
        else
        {
            seq.AppendInterval(punchDuration);
        }

        // 2. Swap: hide full heart, enable broken pieces.
        seq.AppendCallback(() =>
        {
            if (fullSprite != null) fullSprite.gameObject.SetActive(false);
            if (leftSprite != null) leftSprite.gameObject.SetActive(true);
            if (rightSprite != null) rightSprite.gameObject.SetActive(true);
        });

        // 3. Split: pieces move apart + rotate.
        if (leftSprite != null)
        {
            Transform t = leftSprite.transform;
            seq.Append(t.DOLocalMove(_leftRestPos + new Vector3(-splitDistance, 0f, 0f), splitDuration).SetEase(Ease.OutCubic));
            seq.Join(t.DOLocalRotate(new Vector3(0f, 0f, splitRotation), splitDuration).SetEase(Ease.OutCubic));
        }
        if (rightSprite != null)
        {
            Transform t = rightSprite.transform;
            seq.Join(t.DOLocalMove(_rightRestPos + new Vector3(splitDistance, 0f, 0f), splitDuration).SetEase(Ease.OutCubic));
            seq.Join(t.DOLocalRotate(new Vector3(0f, 0f, -splitRotation), splitDuration).SetEase(Ease.OutCubic));
        }

        // 4. Fade out + shrink.
        if (leftSprite != null)
        {
            seq.Append(leftSprite.DOFade(0f, fadeDuration));
            seq.Join(leftSprite.transform.DOScale(_leftRestScale * fadeEndScale, fadeDuration));
        }
        if (rightSprite != null)
        {
            seq.Join(rightSprite.DOFade(0f, fadeDuration));
            seq.Join(rightSprite.transform.DOScale(_rightRestScale * fadeEndScale, fadeDuration));
        }

        // 5. Hide everything and snap back to rest for the next call.
        seq.OnComplete(() =>
        {
            if (leftSprite != null) leftSprite.gameObject.SetActive(false);
            if (rightSprite != null) rightSprite.gameObject.SetActive(false);
            if (fullSprite != null) fullSprite.gameObject.SetActive(false);
            ResetPiecesToRest();
        });
    }

    private void ResetPiecesToRest()
    {
        if (leftSprite != null)
        {
            Transform t = leftSprite.transform;
            t.localPosition = _leftRestPos;
            t.localScale = _leftRestScale;
            t.localRotation = _leftRestRotation;
        }
        if (rightSprite != null)
        {
            Transform t = rightSprite.transform;
            t.localPosition = _rightRestPos;
            t.localScale = _rightRestScale;
            t.localRotation = _rightRestRotation;
        }
        if (fullSprite != null)
            fullSprite.transform.localScale = _fullRestScale;
    }

    private static void SetAlpha(SpriteRenderer sr, float a)
    {
        Color c = sr.color;
        c.a = a;
        sr.color = c;
    }

    private void KillTweens()
    {
        if (fullSprite != null) { fullSprite.transform.DOKill(); fullSprite.DOKill(); }
        if (leftSprite != null) { leftSprite.transform.DOKill(); leftSprite.DOKill(); }
        if (rightSprite != null) { rightSprite.transform.DOKill(); rightSprite.DOKill(); }
    }

    private void OnDestroy()
    {
        KillTweens();
    }
}
