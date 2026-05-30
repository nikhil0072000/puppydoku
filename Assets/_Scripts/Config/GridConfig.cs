using UnityEngine;

[CreateAssetMenu(fileName = "GridConfig", menuName = "PuppyPuzzle/Grid Config")]
public class GridConfig : ScriptableObject
{
    [System.Serializable]
    public struct ColorEntry
    {
        public ColorID id;
        public Color color;
    }

    [SerializeField]
    private ColorEntry[] colorMap;

    // Get colour for a given enum value
    public Color GetColor(ColorID id)
    {
        foreach (var entry in colorMap)
        {
            if (entry.id == id)
                return entry.color;
        }
        Debug.LogWarning($"ColorID {id} not found in GridConfig, using white.");
        return Color.white;
    }

    // Helper to create a default config at runtime (useful for testing without a .asset)
    public static GridConfig CreateDefault()
    {
        var cfg = ScriptableObject.CreateInstance<GridConfig>();
        cfg.colorMap = new ColorEntry[]
        {
            new ColorEntry { id = ColorID.Yellow,   color = Color.yellow },
            new ColorEntry { id = ColorID.Pink,     color = new Color(1f, 0.41f, 0.71f) }, // pink
            new ColorEntry { id = ColorID.DarkPink, color = new Color(0.8f, 0.2f, 0.5f) }, // dark pink
            new ColorEntry { id = ColorID.Purple,   color = new Color(0.5f, 0f, 0.5f) } // purple
        };
        return cfg;
    }
}
