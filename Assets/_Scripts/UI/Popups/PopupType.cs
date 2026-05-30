/// <summary>
/// Identifies every popup the game can show. Used as the key in
/// <see cref="PopupsConfig"/> (one prefab per value) and as the argument to
/// <see cref="PopupCanvasManager"/> show/hide calls.
/// </summary>
public enum PopupType
{
    Settings,
    Victory,
    Defeat,
    LifePanel,
    UserProfilePanel,
    ShopPanel
}
