using UnityEngine;

/// <summary>
/// Lightweight singleton that holds the current theme.<br/>
/// <br/>
/// Visual scripts should read from <see cref="Current"/> only.<br/>
/// Gameplay scripts should NOT reference this class.<br/>
/// <br/>
/// Add this to a GameObject in the first loaded scene (Loading or Home).
/// It survives across scenes via <see cref="DontDestroyOnLoad"/>.
/// </summary>
public class ThemeManager : MonoBehaviour
{
    public static ThemeManager Instance { get; private set; }

    [Header("Theme")]
    [SerializeField] private ThemeData currentTheme;

    /// <summary>The currently active theme. Safe to call from any visual script.</summary>
    public static ThemeData Current => Instance != null ? Instance.currentTheme : null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Switch the active theme at runtime. Invokes <see cref="OnThemeChanged"/>.
    /// </summary>
    public void SetTheme(ThemeData theme)
    {
        if (theme == null) return;
        currentTheme = theme;
        OnThemeChanged?.Invoke();
    }

    /// <summary>
    /// Raised whenever <see cref="SetTheme"/> is called. Visual scripts can
    /// subscribe to react to runtime theme swaps.
    /// </summary>
    public static event System.Action OnThemeChanged;
}
