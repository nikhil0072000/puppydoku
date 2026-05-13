using UnityEngine;
using UnityEngine.SceneManagement;

public class HomeUI : MonoBehaviour
{
    public void OnPlayClicked()
    {
        if (LevelLoader.Instance != null)
            LevelLoader.Instance.GoToGameScene();
        else
            Debug.LogError("LevelLoader instance not found!");
    }
}
