using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// A single Home-scene tab button. Holds two visual states (normal / focused) and
/// exposes its <see cref="Button"/> so <see cref="NavigationManager"/> — the single
/// authority — can wire clicks and drive focus across all tabs at once.
/// </summary>
[RequireComponent(typeof(Button))]
public class HomeTabButton : MonoBehaviour
{
    [Header("State Visuals")]
    [Tooltip("Shown when this tab is NOT selected.")]
    [SerializeField] private GameObject normalState;
    [Tooltip("Shown when this tab IS selected.")]
    [SerializeField] private GameObject focusState;

    private Button _button;

    /// <summary>The Button on this GameObject; NavigationManager subscribes to its onClick.</summary>
    public Button Button
    {
        get
        {
            if (_button == null)
                _button = GetComponent<Button>();
            return _button;
        }
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
    }

    /// <summary>Swaps between the normal and focused visuals.</summary>
    public void SetFocused(bool focused)
    {
        if (normalState != null) normalState.SetActive(!focused);
        if (focusState != null) focusState.SetActive(focused);
    }
}
