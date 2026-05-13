using UnityEngine;

// Simple test script to log win/lose condition messages.
// Attach this to any GameObject in the scene for manual testing.
public class TestScript : MonoBehaviour
{
    private void Start()
    {
        Debug.Log("[TestScript] Start: Ready to test win/lose conditions.");
    }

    // Call this method to simulate a win condition.
    public void TestWin()
    {
        Debug.Log("[TestScript] Win condition triggered – you would see a victory message here.");
    }

    // Call this method to simulate a loss condition.
    public void TestLose()
    {
        Debug.Log("[TestScript] Lose condition triggered – you would see a game‑over message here.");
    }
}
