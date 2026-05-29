using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

public class SettingsToogle : MonoBehaviour
{
    [Header("Toggle State")]
    [SerializeField] private bool isOn = true;

    [Header("UI References")]
    [SerializeField] private RectTransform handle;
    [SerializeField] private Image fillArea;
    [SerializeField] private Image onOffTextImage;

    [Header("Sprites")]
    [SerializeField] private Sprite onSprite;
    [SerializeField] private Sprite offSprite;

    [Header("Colors")]
    [SerializeField] private Color onColor = Color.green;
    [SerializeField] private Color offColor = Color.gray;

    [Header("Handle Positions")]
    [SerializeField] private float handleOnX = 55f;
    [SerializeField] private float handleOffX = -55f;

    [Header("Animation")]
    [SerializeField] private float duration = 0.2f;

    [Header("Type")]
    [SerializeField] private bool isSoundToggle;
    [SerializeField] private bool isHapticToggle;

    // PlayerPrefs keys for the two persisted settings. Other systems read these (e.g. a
    // haptic helper checks HapticKey before vibrating); sound is applied here via AudioListener.
    public const string SoundPrefKey = "Settings.Sound";
    public const string HapticPrefKey = "Settings.Haptic";

    private bool isAnimating;

    private void Start()
    {
        LoadState();
        UpdateVisualInstant();
        ApplySetting();
    }

    private void OnDestroy()
    {
        // Kill any tweens (and the DelayedCall) still running, so their callbacks can't
        // fire on destroyed RectTransforms after a scene unload.
        DOTween.Kill(gameObject);
    }

    private void LoadState()
    {
        if (isSoundToggle)
            isOn = PlayerPrefs.GetInt(SoundPrefKey, 1) == 1;
        else if (isHapticToggle)
            isOn = PlayerPrefs.GetInt(HapticPrefKey, 1) == 1;
    }

    private void ApplySetting()
    {
        if (isSoundToggle)
            AudioListener.volume = isOn ? 1f : 0f;
        // Haptic is consumed by the haptic helper at call sites; only persistence is needed here.
    }

    public void Toggle()
    {
        if (isAnimating) return;

        isOn = !isOn;
        AnimateToggle();

        if (isSoundToggle)
            PlayerPrefs.SetInt(SoundPrefKey, isOn ? 1 : 0);
        else if (isHapticToggle)
            PlayerPrefs.SetInt(HapticPrefKey, isOn ? 1 : 0);

        if (isSoundToggle || isHapticToggle)
            PlayerPrefs.Save();

        ApplySetting();
    }

    private void AnimateToggle()
    {
        isAnimating = true;

        float targetX = isOn ? handleOnX : handleOffX;

        // All tweens are linked to this GameObject so they're killed automatically if it's
        // destroyed mid-animation (prevents callbacks writing to destroyed RectTransforms).
        handle.DOKill();
        handle.DOAnchorPosX(targetX, duration).SetEase(Ease.OutBack)
              .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        Sequence handleSeq = DOTween.Sequence();
        handleSeq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        handleSeq.Append(handle.DOScale(0.85f, 0.08f));
        handleSeq.Append(handle.DOScale(1f, 0.12f));

        Color targetColor = isOn ? onColor : offColor;
        fillArea.DOColor(targetColor, duration)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy);

        onOffTextImage.sprite = isOn ? onSprite : offSprite;

        fillArea.rectTransform
            .DOScale(1.05f, 0.1f)
            .SetLink(gameObject, LinkBehaviour.KillOnDestroy)
            .OnComplete(() => fillArea.rectTransform.DOScale(1f, 0.1f)
                .SetLink(gameObject, LinkBehaviour.KillOnDestroy));

        DOVirtual.DelayedCall(duration, () => isAnimating = false)
                 .SetLink(gameObject, LinkBehaviour.KillOnDestroy);
    }

    private void UpdateVisualInstant()
    {
        Vector2 pos = handle.anchoredPosition;
        pos.x = isOn ? handleOnX : handleOffX;
        handle.anchoredPosition = pos;

        fillArea.color = isOn ? onColor : offColor;
        onOffTextImage.sprite = isOn ? onSprite : offSprite;
    }
}
