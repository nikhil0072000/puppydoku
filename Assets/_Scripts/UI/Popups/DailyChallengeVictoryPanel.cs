using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

/// <summary>
/// Victory popup for daily challenge levels. GameManager.Win() shows it through
/// <see cref="PopupCanvasManager"/> and passes the run's time and percentile via
/// <see cref="Setup"/> right after Show. Continue moves the player back into the
/// normal progression.
/// </summary>
public class DailyChallengeVictoryPanel : Popup
{
    [Header("Daily Victory Panel")]
    [SerializeField] private TextMeshProUGUI titleText;
    [Tooltip("Completion time, e.g. \"Time: 01:24\".")]
    [SerializeField] private TextMeshProUGUI timeText;
    [Tooltip("Percentile line, e.g. \"Beat 82% of players\".")]
    [SerializeField] private TextMeshProUGUI percentileText;
    [Tooltip("Continues into the next normal level.")]
    [SerializeField] private Button continueButton;
    [SerializeField] private Button homeButton;

    private float _elapsedSeconds;
    private int _percentile;

    protected override void Awake()
    {
        base.Awake();
        if (continueButton != null) continueButton.onClick.AddListener(OnContinueClicked);
        if (homeButton != null) homeButton.onClick.AddListener(OnHomeClicked);
    }

    private void OnDestroy()
    {
        if (continueButton != null) continueButton.onClick.RemoveListener(OnContinueClicked);
        if (homeButton != null) homeButton.onClick.RemoveListener(OnHomeClicked);
    }

    /// <summary>Run results shown when the popup opens. Call right after Show().</summary>
    public void Setup(float elapsedSeconds, int percentile)
    {
        _elapsedSeconds = elapsedSeconds;
        _percentile = percentile;
    }

    protected override void OnOpened()
    {
        if (titleText != null)
            titleText.text = "Challenge Cleared";

        if (timeText != null)
        {
            int minutes = Mathf.FloorToInt(_elapsedSeconds / 60f);
            int seconds = Mathf.FloorToInt(_elapsedSeconds % 60f);
            timeText.text = $"Time: {minutes:00}:{seconds:00}";
        }

        if (percentileText != null)
            percentileText.text = $"Beat {_percentile}% of players";
    }

    private void OnContinueClicked()
    {
        PlayClick();
        // The popup canvas persists across scenes — close before leaving.
        RequestClose();

        if (LevelLoader.Instance != null)
            LevelLoader.Instance.LoadNextNormalLevel();
        else
            Debug.LogError("LevelLoader instance not found!");
    }

    private void OnHomeClicked()
    {
        PlayClick();
        RequestClose();
        SceneManager.LoadScene("Home");
    }
}
