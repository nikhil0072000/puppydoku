using UnityEngine;

public class HomeUI : MonoBehaviour
{
    public void OnPlayClicked()
    {
        if (LevelLoader.Instance != null)
            LevelLoader.Instance.LoadFirstAvailableLevel();
        else
            Debug.LogError("LevelLoader instance not found!");
    }
}
