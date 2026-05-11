using UnityEngine;
using System.Collections.Generic;

public class HintPowerUp : MonoBehaviour
{
    [Header("Hint Settings")]
    public int hintUses = 3;
    public float hintGlowDuration = 2f;
    public Color hintColor = Color.yellow;
    public float hintPulseSpeed = 2f;
    
    [Header("Visual Effects")]
    public GameObject hintIndicator;
    public ParticleSystem hintParticles;
    
    private int currentHints;
    private bool isShowingHint = false;

    void Start()
    {
        currentHints = hintUses;
    }

    public bool UseHint()
    {
        if (currentHints <= 0 || isShowingHint)
            return false;
            
        currentHints--;
        ShowHint();
        return true;
    }

    void ShowHint()
    {
        // Find a cell from the solution that is empty and not yet correctly placed
        Vector2Int? hintCell = FindBestHintCell();
        
        if (hintCell.HasValue)
        {
            isShowingHint = true;
            Cell targetCell = GameManager.Instance.gridManager.Cells[hintCell.Value.x, hintCell.Value.y];
            StartCoroutine(ShowHintEffect(targetCell));
        }
    }

    Vector2Int? FindBestHintCell()
    {
        var puzzle = GameManager.Instance.currentPuzzle;
        var placedCats = GameManager.Instance.playerPlacedCats;
        var grid = GameManager.Instance.gridManager.Cells;

        foreach (var solCat in puzzle.solution)
        {
            // Skip if already placed correctly
            if (placedCats.Contains(solCat))
                continue;
                
            // Skip if cell already has a cat (wrong placement)
            if (grid[solCat.x, solCat.y].hasCat)
                continue;
                
            // This is a good hint candidate
            return solCat;
        }
        
        return null;
    }

    System.Collections.IEnumerator ShowHintEffect(Cell targetCell)
    {
        float elapsed = 0f;
        Vector3 originalScale = targetCell.transform.localScale;
        Color originalColor = targetCell.zoneOverlay.color;
        
        // Create hint indicator if needed
        if (hintIndicator != null)
        {
            GameObject indicator = Instantiate(hintIndicator, targetCell.transform);
            indicator.transform.localPosition = Vector3.zero;
            
            // Glow effect
            while (elapsed < hintGlowDuration)
            {
                float pulse = Mathf.Sin(Time.time * hintPulseSpeed) * 0.3f + 0.7f;
                targetCell.zoneOverlay.color = Color.Lerp(originalColor, hintColor, pulse);
                targetCell.transform.localScale = originalScale * (1f + pulse * 0.1f);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            // Restore original
            targetCell.zoneOverlay.color = originalColor;
            targetCell.transform.localScale = originalScale;
            Destroy(indicator);
        }
        else
        {
            // Simple color pulse without indicator
            while (elapsed < hintGlowDuration)
            {
                float pulse = Mathf.Sin(Time.time * hintPulseSpeed) * 0.3f + 0.7f;
                targetCell.zoneOverlay.color = Color.Lerp(originalColor, hintColor, pulse);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            targetCell.zoneOverlay.color = originalColor;
        }
        
        // Play particles if available
        if (hintParticles != null)
        {
            ParticleSystem particles = Instantiate(hintParticles, targetCell.transform.position, Quaternion.identity);
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }
        
        isShowingHint = false;
    }

    public int GetRemainingHints()
    {
        return currentHints;
    }

    public void AddHints(int amount)
    {
        currentHints = Mathf.Min(currentHints + amount, hintUses);
    }

    public void ResetHints()
    {
        currentHints = hintUses;
    }
}
