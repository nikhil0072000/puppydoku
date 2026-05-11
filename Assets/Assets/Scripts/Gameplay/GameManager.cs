using System.Collections.Generic;
using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("Game State")]
    public int currentLevel = 1;
    public int lives = 3;
    public int maxLives = 3;
    public HashSet<Vector2Int> playerPlacedCats = new HashSet<Vector2Int>();
    public PuzzleData currentPuzzle;
    public GridManager gridManager;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    void Start()
    {
        LoadLevel(currentLevel);
    }

    public void LoadLevel(int level)
    {
        currentLevel = level;
        lives = maxLives;
        playerPlacedCats.Clear();

        currentPuzzle = PuzzleGenerator.GeneratePuzzle(level);
        gridManager.GenerateGrid(currentPuzzle);

        // Place given cats on board
        foreach (var catPos in currentPuzzle.givenCats)
        {
            gridManager.Cells[catPos.x, catPos.y].PlaceCat(cat: true, isGiven: true);
        }
    }

    public void OnCellTapped(Vector2Int cellPos)
    {
        Cell cell = gridManager.Cells[cellPos.x, cellPos.y];
        if (cell.isGiven) return; // can't remove given cats

        if (cell.hasCat)
        {
            // Remove cat
            RemoveCat(cellPos);
        }
        else
        {
            // Try to place cat
            if (IsPlacementValid(cellPos))
            {
                PlaceCat(cellPos);
                CheckWinCondition();
            }
            else
            {
                // Wrong placement! Lose a life.
                lives--;
                HUDManager.Instance.UpdateHearts(lives);
                // Animation for mistake
                cell.ShowIncorrectPlacement();
                if (lives <= 0)
                {
                    GameOver();
                }
            }
        }
    }

    bool IsPlacementValid(Vector2Int pos)
    {
        return GameRules.IsPlacementValid(pos, playerPlacedCats, currentPuzzle.zoneMap);
    }

    void PlaceCat(Vector2Int pos)
    {
        playerPlacedCats.Add(pos);
        gridManager.Cells[pos.x, pos.y].PlaceCat(cat: true, isGiven: false);
    }

    void RemoveCat(Vector2Int pos)
    {
        playerPlacedCats.Remove(pos);
        gridManager.Cells[pos.x, pos.y].RemoveCat();
    }

    void CheckWinCondition()
    {
        // Win if all zones have a cat AND all placements match solution? Or just count cats?
        // We can check whether player placed exactly one cat per zone.
        HashSet<int> zonesFilled = new HashSet<int>();
        foreach (var cat in playerPlacedCats)
        {
            int zone = currentPuzzle.zoneMap[cat.x, cat.y];
            if (zonesFilled.Contains(zone)) return; // duplicate in zone, shouldn't happen
            zonesFilled.Add(zone);
        }
        if (zonesFilled.Count == currentPuzzle.gridSize) // gridSize = number of zones
        {
            // Victory! Even if solution not identical, it's valid if all rules satisfied.
            // But to be strict, we can compare with puzzle solution.
            Debug.Log("Level Complete!");
            // Load next level or show win screen
        }
    }

    void GameOver()
    {
        Debug.Log("Game Over");
        // Show game over screen, retry button
    }
}
