using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Defeat popup for Tutorial/Normal levels (daily challenges use
/// <see cref="DailyChallengeFailPanel"/>). Shows the failed level's number and a
/// single Retry button that restarts the level.
/// </summary>
public class DefeatPanel : Popup
{
    [Header("Defeat Panel")]
    [Tooltip("Displays the failed level's number, e.g. \"3\".")]
    [SerializeField] private TextMeshProUGUI levelNumberTxt;
    [Tooltip("Restarts the level (reloads the Game scene).")]
    [SerializeField] private Button retryButton;

    protected override void Awake()
    {
        base.Awake();
        if (retryButton != null) retryButton.onClick.AddListener(OnRetryClicked);
    }

    private void OnDestroy()
    {
        if (retryButton != null) retryButton.onClick.RemoveListener(OnRetryClicked);
    }

    protected override void OnOpened()
    {
        if (levelNumberTxt != null)
        {
            levelNumberTxt.text = LevelLoader.Instance != null
                ? LevelLoader.Instance.CurrentNormalLevelNumber.ToString()
                : "1";
        }
    }

    private void OnRetryClicked()
    {
        PlayClick();
        // The popup canvas persists across scenes — close before leaving.
        RequestClose();
        // Reload the active scene; level data is retained in LevelLoader.
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
