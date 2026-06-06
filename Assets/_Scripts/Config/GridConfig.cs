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
            new ColorEntry { id = ColorID.Yellow,    color = new Color32(232, 207, 119, 255) },
            new ColorEntry { id = ColorID.Pink,      color = new Color32(227, 138, 217, 255) },
            new ColorEntry { id = ColorID.DarkPink,  color = new Color32(207, 113, 149, 255) },
            new ColorEntry { id = ColorID.Purple,    color = new Color32(138, 122, 219, 255) },
            new ColorEntry { id = ColorID.LightGreen, color = new Color32(139, 208, 123, 255) },
            new ColorEntry { id = ColorID.DarkGreen, color = new Color32(47, 153, 85, 255) },
            new ColorEntry { id = ColorID.Cyan,      color = new Color32(67, 169, 191, 255) },
            new ColorEntry { id = ColorID.LightBlue, color = new Color32(166, 195, 227, 255) },
            new ColorEntry { id = ColorID.Blue,      color = new Color32(94, 136, 196, 255) },
            new ColorEntry { id = ColorID.Brown,     color = new Color32(180, 121, 82, 255) },
            new ColorEntry { id = ColorID.Orange,    color = new Color32(244, 161, 93, 255) },
            new ColorEntry { id = ColorID.Gold,      color = new Color32(216, 179, 0, 255) }
        };
        return cfg;
    }
}
