using UnityEngine;
using UnityEngine.UI;

public class PowerUpManager : MonoBehaviour
{
    public static PowerUpManager Instance;
    
    public Button hintButton;
    public Button extraLifeButton;
    public Button regionCheckButton;

    public int hintUses = 3;
    public int extraLivesAvailable = 2;
    public int regionChecks = 2;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        hintButton.onClick.AddListener(UseHint);
        extraLifeButton.onClick.AddListener(UseExtraLife);
        regionCheckButton.onClick.AddListener(UseRegionCheck);
    }

    public void UseHint()
    {
        if (hintUses <= 0) return;
        // Find a cell from the solution that is empty and not yet correctly placed
        foreach (var solCat in GameManager.Instance.currentPuzzle.solution)
        {
            if (!GameManager.Instance.playerPlacedCats.Contains(solCat) &&
                !GameManager.Instance.gridManager.Cells[solCat.x, solCat.y].hasCat)
            {
                // Show a ghost cat or highlight
                GameManager.Instance.gridManager.Cells[solCat.x, solCat.y].ShowHint();
                hintUses--;
                break;
            }
        }
    }

    public void UseExtraLife()
    {
        if (extraLivesAvailable <= 0) return;
        GameManager.Instance.lives = Mathf.Min(GameManager.Instance.lives + 1, GameManager.Instance.maxLives);
        extraLivesAvailable--;
        HUDManager.Instance.UpdateHearts(GameManager.Instance.lives);
    }

    public void UseRegionCheck()
    {
        if (regionChecks <= 0) return;
        // Highlight a random region that has conflicts or is empty?
        // More useful: visually check if a region's current placements are valid (no row/col/diag conflicts with other cats)
        // We'll just flash the region.
        regionChecks--;
    }
}
