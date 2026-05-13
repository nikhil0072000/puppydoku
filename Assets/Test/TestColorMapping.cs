using UnityEngine;

// Simple test script that demonstrates the GridConfig colour mapping.
// Attach this to any GameObject in a scene to see the colour logged.
public class TestColorMapping : MonoBehaviour
{
    // You can assign a GridConfig asset in the inspector, or leave null to use the default.
    public GridConfig config;

    void Start()
    {
        // If no asset assigned, create a default config at runtime.
        if (config == null)
        {
            config = GridConfig.CreateDefault();
            Debug.Log("[TestColorMapping] Created default GridConfig at runtime.");
        }

        // Example: log the colour for Purple (enum value 3).
        var colour = config.GetColor(ColorID.Purple);
        Debug.Log($"[TestColorMapping] Colour for ColorID.Purple is {colour}");
    }
}
