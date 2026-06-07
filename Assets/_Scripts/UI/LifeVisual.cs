using UnityEngine;

/// <summary>
/// One life slot on the Game-scene HUD. The prefab carries two visuals — the
/// "alive" heart and the "lost" heart — and this script flips between them.
/// Instantiated and driven by <see cref="GameLivesManager"/>.
/// </summary>
public class LifeVisual : MonoBehaviour
{
    [Tooltip("Visual shown while this life is still available.")]
    [SerializeField] private GameObject lifeActiveVisual;
    [Tooltip("Visual shown after this life has been lost.")]
    [SerializeField] private GameObject lifeLostVisual;

    /// <summary>True while this slot still shows an available life.</summary>
    public bool IsAlive { get; private set; } = true;

    /// <summary>Shows the active-heart visual.</summary>
    public void MarkAlive()
    {
        IsAlive = true;
        ApplyVisual();
    }

    /// <summary>Shows the lost-heart visual.</summary>
    public void MarkLost()
    {
        IsAlive = false;
        ApplyVisual();
    }

    private void ApplyVisual()
    {
        if (lifeActiveVisual != null) lifeActiveVisual.SetActive(IsAlive);
        if (lifeLostVisual != null) lifeLostVisual.SetActive(!IsAlive);
    }
}
