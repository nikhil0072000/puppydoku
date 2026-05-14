using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class PopupManager : MonoBehaviour
{
    public static PopupManager Instance { get; private set; }

    [Header("Popup References")]
    [SerializeField] private GameObject popupPanel;        // the root WinLosePopup
    [SerializeField] private TextMeshProUGUI messageText;
    [SerializeField] private GameObject retryButton;
    [SerializeField] private GameObject homeButton;
    [SerializeField] private GameObject nextLevelButton;   // visible only on win and only if another level exists

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    void Start()
    {
        // Ensure popup is hidden at start
        if (popupPanel != null) popupPanel.SetActive(false);
    }

    public void ShowWin()
    {
        if (popupPanel != null) popupPanel.SetActive(true);

        bool hasNext = LevelLoader.Instance != null && LevelLoader.Instance.HasNextLevel;

        if (messageText != null)
            messageText.text = hasNext ? "🎉 Level Complete!" : "🏆 All Levels Complete!";

        if (retryButton != null) retryButton.SetActive(true);
        if (homeButton != null) homeButton.SetActive(true);
        if (nextLevelButton != null) nextLevelButton.SetActive(hasNext);
    }

    public void ShowLose()
    {
        if (popupPanel != null) popupPanel.SetActive(true);
        if (messageText != null) messageText.text = "💔 Out of Lives!";
        if (retryButton != null) retryButton.SetActive(true);
        if (homeButton != null) homeButton.SetActive(true);
        if (nextLevelButton != null) nextLevelButton.SetActive(false);
    }

    // Called by Retry button OnClick
    public void OnRetryClicked()
    {
        // Reload the Game scene (level data still in LevelLoader)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // Called by Home button OnClick
    public void OnHomeClicked()
    {
        SceneManager.LoadScene("Home");
    }

    // Called by Next Level button OnClick
    public void OnNextLevelClicked()
    {
        if (LevelLoader.Instance != null)
            LevelLoader.Instance.LoadNextLevel();
        else
            Debug.LogError("LevelLoader instance not found!");
    }
}
