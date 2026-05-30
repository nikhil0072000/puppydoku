using UnityEngine;

public class GridConfigTest : MonoBehaviour
{
    void Start()
    {
        GridConfig config = Resources.Load<GridConfig>("GridConfig");
        if (config == null)
        {
            Debug.LogError("GridConfig asset not found in Resources! Make sure it's named 'GridConfig'.");
            return;
        }

        // Test: get colour for Pink (ID = 1)
        Color pink = config.GetColor(ColorID.Pink);
        Debug.Log($"Module 2 Test – Pink colour: {pink}");

        // Optional: test a missing ID (should log warning and return white)
        Color dummy = config.GetColor((ColorID)99);
        Debug.Log($"Missing ID test – colour: {dummy}");
    }
}
