using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The Bulb-hint popup: shows the hint message for the current hint step and an
/// Apply button. HintBooster configures it via <see cref="Setup"/> right after
/// showing it through <see cref="PopupCanvasManager"/>.
///
/// Apply → the apply callback runs (commit X's / place the puppy) and the popup
/// closes. Closing any other way (X button, preemption by another popup) fires
/// the dismissed callback so the booster can clean up and refund.
/// </summary>
public class HintPopup : Popup
{
    [Header("Hint Popup")]
    [Tooltip("Displays the current hint's message.")]
    [SerializeField] private TextMeshProUGUI hintText;
    [Tooltip("Applies the hint (commits the X marks / places the puppy) and closes.")]
    [SerializeField] private Button applyButton;

    private Action _onApply;
    private Action _onDismissed;
    private bool _applied;

    protected override void Awake()
    {
        base.Awake();
        if (applyButton != null) applyButton.onClick.AddListener(OnApplyClicked);
    }

    private void OnDestroy()
    {
        if (applyButton != null) applyButton.onClick.RemoveListener(OnApplyClicked);
    }

    /// <summary>Configures this popup for one hint step. Call right after Show().</summary>
    public void Setup(string message, Action onApply, Action onDismissed)
    {
        _onApply = onApply;
        _onDismissed = onDismissed;
        _applied = false;

        if (hintText != null) hintText.text = message;
        if (applyButton != null) applyButton.interactable = true;
    }

    private void OnApplyClicked()
    {
        if (_applied) return;
        _applied = true;

        PlayClick();
        if (applyButton != null) applyButton.interactable = false;

        Action apply = _onApply;
        _onApply = null;
        apply?.Invoke();

        RequestClose();
    }

    protected override void OnClosed()
    {
        Action dismissed = _onDismissed;
        _onApply = null;
        _onDismissed = null;

        // Closed without Apply (X button / preempted by another popup) → cancel.
        if (!_applied && dismissed != null)
            dismissed();
    }
}
