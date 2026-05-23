using UnityEngine;
using TMPro;

public class HomeUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI levelLabelText;

    private void Start()
    {
        RefreshLevelLabel();
    }

    private void OnEnable()
    {
        RefreshLevelLabel();
    }

    private void RefreshLevelLabel()
    {
        if (levelLabelText == null)
            return;

        if (LevelLoader.Instance != null)
            levelLabelText.text = LevelLoader.Instance.CurrentLevelButtonLabel;
        else
            levelLabelText.text = "Level 1";
    }

    public void OnPlayClicked()
    {
        if (LevelLoader.Instance != null)
            LevelLoader.Instance.LoadCurrentLevel();
        else
            Debug.LogError("LevelLoader instance not found!");
    }
}
