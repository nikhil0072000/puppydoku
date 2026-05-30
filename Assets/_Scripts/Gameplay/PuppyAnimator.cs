using DG.Tweening;
using UnityEngine;

/// <summary>
/// Drives the per-puppy reactions: a wink (with scale-in → hold → scale-back,
/// sparkle VFX, SFX) on a successful placement, and a sad face when the player
/// makes a wrong move. State transitions back to Idle are handled inside the
/// Animator Controller (Wink → Idle and Sad → Idle have Has Exit Time = true),
/// so no coroutine work is needed here.
/// </summary>
[RequireComponent(typeof(Animator))]
public class PuppyAnimator : MonoBehaviour
{
    [Header("References")]
    [Tooltip("Animator on this GameObject. Auto-assigned in Awake if left empty.")]
    [SerializeField] private Animator _animator;

    [Header("Wink Scale")]
    [Tooltip("Scale multiplier reached during the wink (1.2 = 20% larger than the idle scale).")]
    [Min(1f)]
    [SerializeField] private float _winkScaleMultiplier = 1.2f;
    [Tooltip("How long the scale-in (grow) tween takes.")]
    [Min(0f)]
    [SerializeField] private float _scaleInDuration = 0.15f;
    [Tooltip("Hold at the scaled-up size for this long before scaling back. Tune so (scaleIn + hold + scaleOut) ≈ Puppy_Wink.anim length (0.67s) — that way the puppy returns to original right as the wink finishes.")]
    [Min(0f)]
    [SerializeField] private float _scaleHoldDuration = 0.37f;
    [Tooltip("How long the scale-back to original takes.")]
    [Min(0f)]
    [SerializeField] private float _scaleOutDuration = 0.15f;
    [Tooltip("Wait this long before starting the scale-in so the Cell.PlacePuppy pop-in (0→1.1x→1x, ~0.25s) finishes first. If 0, this tween fights the pop-in and the puppy ends up tiny.")]
    [Min(0f)]
    [SerializeField] private float _scaleInDelay = 0.3f;

    // Cached parameter hashes (Animator string lookups allocate; do it once).
    private static readonly int WinkHash = Animator.StringToHash("PlayWink");
    private static readonly int SadHash  = Animator.StringToHash("PlaySad");

    private SpriteRenderer _spriteRenderer;
    private Vector3 _idleScale;

    private void Awake()
    {
        if (_animator == null) TryGetComponent(out _animator);
        TryGetComponent(out _spriteRenderer);
        _idleScale = transform.localScale;
    }

    private void Start()
    {
        ApplyTheme();
    }

    private void ApplyTheme()
    {
        ThemeData theme = ThemeManager.Current;
        if (theme == null) return;

        if (theme.puppyAnimatorController != null && _animator != null)
            _animator.runtimeAnimatorController = theme.puppyAnimatorController;

        if (theme.puppyIdleSprite != null && _spriteRenderer != null)
            _spriteRenderer.sprite = theme.puppyIdleSprite;
    }

    public void PlayWink()
    {
#if UNITY_EDITOR
        Debug.Log($"[PuppyAnimator] PlayWink on '{gameObject.name}' at {transform.position} → trigger SFX={SFXType.Wink}, VFX={VFXType.Wink}", this);
#endif
        if (_animator != null) _animator.SetTrigger(WinkHash);

        // IMPORTANT: do NOT DOKill the transform here. When GameManager triggers
        // PlayWink immediately after Cell.PlacePuppy, the placement pop-in
        // (0 → 1.1x → 1x over ~0.25s) is still running. Killing it freezes the
        // puppy at near-zero scale ("tiny puppy" bug). Instead we delay the
        // scale-in so it begins AFTER the pop-in has fully settled.
        Vector3 winkScale = _idleScale * _winkScaleMultiplier;

        DOTween.Sequence()
               .AppendInterval(_scaleInDelay)
               .Append(transform.DOScale(winkScale, _scaleInDuration).SetEase(Ease.OutBack))
               .AppendInterval(_scaleHoldDuration)
               .Append(transform.DOScale(_idleScale, _scaleOutDuration).SetEase(Ease.InOutSine))
               .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        if (VFXManager.Instance != null)
            VFXManager.Instance.Play(VFXType.Wink, transform.position);
#if UNITY_EDITOR
        else Debug.LogWarning($"[PuppyAnimator] '{gameObject.name}' skipped VFX — VFXManager.Instance is null.", this);
#endif

        if (SFXManager.Instance != null)
            SFXManager.Instance.Play(SFXType.Wink);
#if UNITY_EDITOR
        else Debug.LogWarning($"[PuppyAnimator] '{gameObject.name}' skipped SFX — SFXManager.Instance is null.", this);
#endif
    }

    public void PlaySad()
    {
#if UNITY_EDITOR
        Debug.Log($"[PuppyAnimator] PlaySad on '{gameObject.name}' at {transform.position} → trigger SFX={SFXType.Sad}", this);
#endif
        if (_animator != null) _animator.SetTrigger(SadHash);

        if (SFXManager.Instance != null)
            SFXManager.Instance.Play(SFXType.Sad);
#if UNITY_EDITOR
        else Debug.LogWarning($"[PuppyAnimator] '{gameObject.name}' skipped SFX — SFXManager.Instance is null.", this);
#endif
    }

    private void OnDestroy()
    {
        transform.DOKill();
    }
}
