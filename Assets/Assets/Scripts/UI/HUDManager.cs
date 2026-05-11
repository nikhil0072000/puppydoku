using UnityEngine.UI;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    public static HUDManager Instance;
    public Image[] hearts;
    public Sprite fullHeart, emptyHeart;
    public Text levelText;

    void Awake() => Instance = this;

    public void UpdateHearts(int currentLives)
    {
        for (int i = 0; i < hearts.Length; i++)
            hearts[i].sprite = i < currentLives ? fullHeart : emptyHeart;
    }

    public void SetLevel(int level) => levelText.text = "Level " + level;
}
