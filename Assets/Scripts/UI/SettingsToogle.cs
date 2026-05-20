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

    private bool isAnimating;

    private void Start()
    {
        UpdateVisualInstant();
    }

    public void Toggle()
    {
        if (isAnimating) return;

        isOn = !isOn;
        AnimateToggle();

        if (isSoundToggle)
        {
            Debug.Log(isOn ? "Sound ON" : "Sound OFF");
        }

        if (isHapticToggle)
        {
            Debug.Log(isOn ? "Haptic ON" : "Haptic OFF");
        }
    }

    private void AnimateToggle()
    {
        isAnimating = true;

        float targetX = isOn ? handleOnX : handleOffX;

        handle.DOKill();
        handle.DOAnchorPosX(targetX, duration).SetEase(Ease.OutBack);

        Sequence handleSeq = DOTween.Sequence();
        handleSeq.Append(handle.DOScale(0.85f, 0.08f));
        handleSeq.Append(handle.DOScale(1f, 0.12f));

        Color targetColor = isOn ? onColor : offColor;
        fillArea.DOColor(targetColor, duration);

        onOffTextImage.sprite = isOn ? onSprite : offSprite;

        fillArea.rectTransform
            .DOScale(1.05f, 0.1f)
            .OnComplete(() => fillArea.rectTransform.DOScale(1f, 0.1f));

        DOVirtual.DelayedCall(duration, () => isAnimating = false);
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
