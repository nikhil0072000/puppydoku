using UnityEngine;

[CreateAssetMenu(fileName = "ThemeData", menuName = "PuppyPuzzle/Theme Data")]
public class ThemeData : ScriptableObject
{
    [Header("Puppy")]
    [Tooltip("Prefab spawned when a puppy is placed on a cell. Must have PuzzleObject + PuppyAnimator + Animator + SpriteRenderer.")]
    public GameObject puppyPrefab;
    [Tooltip("Animator Controller applied to the puppy at runtime.")]
    public RuntimeAnimatorController puppyAnimatorController;
    [Tooltip("Default idle sprite used if the animator controller is null.")]
    public Sprite puppyIdleSprite;

    [Header("Cell Visuals")]
    [Tooltip("Sprite for the cell zone overlay (colored area).")]
    public Sprite cellZoneOverlaySprite;
    [Tooltip("Optional: cell background shape (if null, keeps the prefab's default).")]
    public Sprite cellBackgroundSprite;
    [Tooltip("Color for the white overlay flash on cell tap.")]
    public Color whiteOverlayColor = Color.white;
    [Tooltip("Color for the X line on error.")]
    public Color errorCrossColor = new Color32(255, 73, 0, 255);
    [Tooltip("Gold glow color for hint focus cell.")]
    public Color hintGlowColor = new Color32(255, 200, 60, 255);

    [Header("Heart Break")]
    [Tooltip("Default: use embedded HeartUI on Cell prefab. Set to true to use theme-specific heart sprites.")]
    public bool useThemeHeartBreak;
    public Sprite heartFullSprite;
    public Sprite heartLeftBrokenSprite;
    public Sprite heartRightBrokenSprite;

    [Header("HUD")]
    public Sprite hudHeartFullSprite;
    public Sprite hudHeartEmptySprite;

    [Header("Background")]
    public Sprite gameBackgroundSprite;
    public Color gameBackgroundColor = Color.white;

    [Header("VFX (Optional Overrides)")]
    [Tooltip("Per-theme VFX config. If null, uses the global VFXConfig.")]
    public VFXConfig vfxConfig;

    [Header("Audio (Optional Overrides)")]
    [Tooltip("Per-theme SFX config. If null, uses the global SFXConfig.")]
    public SFXConfig sfxConfig;
}
