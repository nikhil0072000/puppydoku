using UnityEngine;

public class Cell : MonoBehaviour
{
    // Static zone colour palette (enough for up to 10 zones)
    public static readonly Color[] ZoneColors = new Color[]
    {
        new Color32(255,182,193,200),  // light pink
        new Color32(173,216,230,200),  // light blue
        new Color32(144,238,144,200),  // light green
        new Color32(255,255,160,200),  // light yellow
        new Color32(221,160,221,200),  // plum
        new Color32(244,164,96 ,200),  // sandy brown
        new Color32(152,251,152,200),  // pale green
        new Color32(175,238,238,200),  // turquoise
        new Color32(255,218,185,200),  // peach
        new Color32(230,230,250,200),  // lavender
    };

    [SerializeField] private SpriteRenderer zoneOverlay;   // assigned in prefab
    public Vector2Int gridPosition { get; private set; }
    public int zoneID { get; private set; }

    // Initialise the cell with position and zone
    public void Init(int x, int y, int zone)
    {
        gridPosition = new Vector2Int(x, y);
        zoneID = zone;

        // Apply zone colour
        if (zoneOverlay != null && zone >= 0 && zone < ZoneColors.Length)
            zoneOverlay.color = ZoneColors[zone];
        else
            Debug.LogWarning($"Cell ({x},{y}): invalid zone or missing overlay.");

        // Just for visual check: name the GameObject
        gameObject.name = $"Cell_{x}_{y}_Zone{zone}";
    }
}