using System;
using DG.Tweening;
using UnityEngine;

/// <summary>
/// Base class for every popup. Lives on the prefab root and owns the open/close
/// animation (scale + fade via DOTween) and the show/hide lifecycle. Dedicated
/// controllers (VictoryPanel, SettingsPanel, ...) derive from this and override
/// <see cref="OnOpened"/>/<see cref="OnClosed"/> for their own logic.
///
/// Tweens use unscaled time so popups still animate while gameplay is paused
/// (Time.timeScale == 0), matching FakeAdPanel's unscaled countdown.
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public abstract class Popup : MonoBehaviour
{
    [Header("Popup Base")]
    [Tooltip("The body that scales during open/close. Defaults to this RectTransform if unset.")]
    [SerializeField] private RectTransform content;

    [Tooltip("Set automatically by PopupCanvasManager when instantiated; serialized for prefab clarity.")]
    [SerializeField] private PopupType type;

    [Header("Animation")]
    [Min(0f)][SerializeField] private float openDuration = 0.25f;
    [Min(0f)][SerializeField] private float closeDuration = 0.2f;
    [SerializeField] private Ease openEase = Ease.OutBack;
    [SerializeField] private Ease closeEase = Ease.InBack;
    [Tooltip("Scale the content starts/ends at while hidden.")]
    [SerializeField] private Vector3 hiddenScale = new Vector3(0.7f, 0.7f, 0.7f);

    private CanvasGroup _canvasGroup;

    public PopupType Type => type;

    protected CanvasGroup CanvasGroup => _canvasGroup;

    protected virtual void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        if (content == null)
            content = (RectTransform)transform;
    }

    /// <summary>Assigned by <see cref="PopupCanvasManager"/> on instantiation so the
    /// instance's type always matches the registry key, regardless of prefab value.</summary>
    public void SetType(PopupType value) => type = value;

    /// <summary>Activates and animates the popup in.</summary>
    /// <param name="onComplete">Optional callback invoked after the open animation finishes.</param>
    public void Open(Action onComplete = null)
    {
        gameObject.SetActive(true);
        KillTweens();

        content.localScale = hiddenScale;
        _canvasGroup.alpha = 0f;
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();
        seq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        seq.SetUpdate(true); // unscaled — survives Time.timeScale == 0
        seq.Append(content.DOScale(Vector3.one, openDuration).SetEase(openEase));
        seq.Join(_canvasGroup.DOFade(1f, openDuration));
        seq.OnComplete(() =>
        {
            _canvasGroup.interactable = true;
            _canvasGroup.blocksRaycasts = true;
            OnOpened();
            onComplete?.Invoke();
        });
    }

    /// <summary>Animates the popup out, then deactivates it (instance is kept cached).</summary>
    /// <param name="onClosed">Optional callback invoked after the popup is hidden.</param>
    public void Close(Action onClosed = null)
    {
        KillTweens();
        _canvasGroup.interactable = false;
        _canvasGroup.blocksRaycasts = false;

        Sequence seq = DOTween.Sequence();
        seq.SetLink(gameObject, LinkBehaviour.KillOnDestroy);
        seq.SetUpdate(true);
        seq.Append(content.DOScale(hiddenScale, closeDuration).SetEase(closeEase));
        seq.Join(_canvasGroup.DOFade(0f, closeDuration));
        seq.OnComplete(() =>
        {
            gameObject.SetActive(false);
            OnClosed();
            onClosed?.Invoke();
        });
    }

    /// <summary>Hook for the popup's own close (X) button.</summary>
    public void RequestClose()
    {
        if (PopupCanvasManager.Instance != null)
            PopupCanvasManager.Instance.Hide(type);
        else
            Close();
    }

    /// <summary>Plays the shared button-click SFX. Safe if no SFXManager exists.</summary>
    protected void PlayClick()
    {
        if (SFXManager.Instance != null)
            SFXManager.Instance.Play(SFXType.ButtonClick);
    }

    /// <summary>Called after the open animation completes. Refresh data / subscribe here.</summary>
    protected virtual void OnOpened() { }

    /// <summary>Called after the close animation completes. Unsubscribe / reset here.</summary>
    protected virtual void OnClosed() { }

    private void KillTweens()
    {
        content.DOKill();
        _canvasGroup.DOKill();
    }
}
