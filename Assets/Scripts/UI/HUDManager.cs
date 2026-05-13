using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class HUDManager : MonoBehaviour
{
    [Header("Hearts")]
    [SerializeField] private Image[] heartImages;
    [SerializeField] private Sprite fullHeart;
    [SerializeField] private Sprite emptyHeart;

    [Header("Progress")]
    [SerializeField] private TextMeshProUGUI progressText;

    public void UpdateHearts(int currentLives)
    {
        for (int i = 0; i < heartImages.Length; i++)
        {
            if (heartImages[i] != null)
                heartImages[i].sprite = i < currentLives ? fullHeart : emptyHeart;
        }
    }

    public void UpdateProgress(int placed, int total)
    {
        if (progressText != null)
            progressText.text = $"{placed}/{total}";
    }
}
