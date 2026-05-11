using UnityEngine;
using UnityEngine.UI;


public class ExtraLifePowerUp : MonoBehaviour
{
    [Header("Extra Life Settings")]
    public int extraLivesAvailable = 2;
    public int livesPerUse = 1;
    public float healAnimationDuration = 1f;
    
    [Header("Visual Effects")]
    public GameObject heartEffect;
    public ParticleSystem healParticles;
    public AnimationCurve healCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);
    
    [Header("Audio")]
    public AudioClip healSound;
    
    private int currentExtraLives;
    private bool isHealing = false;

    void Start()
    {
        currentExtraLives = extraLivesAvailable;
    }

    public bool UseExtraLife()
    {
        if (currentExtraLives <= 0 || isHealing)
            return false;
            
        if (GameManager.Instance.lives >= GameManager.Instance.maxLives)
            return false; // Already at max lives
            
        currentExtraLives--;
        StartCoroutine(HealAnimation());
        return true;
    }

    System.Collections.IEnumerator HealAnimation()
    {
        isHealing = true;
        
        int oldLives = GameManager.Instance.lives;
        GameManager.Instance.lives = Mathf.Min(GameManager.Instance.lives + livesPerUse, GameManager.Instance.maxLives);
        
        // Update HUD immediately
        HUDManager.Instance.UpdateHearts(GameManager.Instance.lives);
        
        // Play heal sound
        if (healSound != null)
        {
            AudioSource.PlayClipAtPoint(healSound, Camera.main.transform.position);
        }
        
        // Visual heart effect
        if (heartEffect != null)
        {
            GameObject heart = Instantiate(heartEffect, HUDManager.Instance.hearts[oldLives].transform.position, Quaternion.identity);
            heart.transform.SetParent(HUDManager.Instance.hearts[oldLives].transform);
            
            // Animate heart
            float elapsed = 0f;
            Vector3 originalScale = heart.transform.localScale;
            
            while (elapsed < healAnimationDuration)
            {
                float curveValue = healCurve.Evaluate(elapsed / healAnimationDuration);
                heart.transform.localScale = originalScale * (1f + curveValue * 0.5f);
                
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            heart.transform.localScale = originalScale;
            Destroy(heart);
        }
        
        // Particle effects
        if (healParticles != null)
        {
            ParticleSystem particles = Instantiate(healParticles, Camera.main.transform);
            particles.transform.position = HUDManager.Instance.hearts[oldLives].transform.position;
            particles.Play();
            Destroy(particles.gameObject, particles.main.duration);
        }
        
        // Update heart UI with animation
        StartCoroutine(AnimateHeartFill(oldLives));
        
        isHealing = false;
    }

    System.Collections.IEnumerator AnimateHeartFill(int heartIndex)
    {
        if (heartIndex < HUDManager.Instance.hearts.Length)
        {
            Image heartImage = HUDManager.Instance.hearts[heartIndex];
            heartImage.sprite = HUDManager.Instance.fullHeart;
            
            // Bounce effect
            Vector3 originalScale = heartImage.transform.localScale;
            heartImage.transform.localScale = originalScale * 1.3f;
            
            float elapsed = 0f;
            while (elapsed < 0.3f)
            {
                heartImage.transform.localScale = Vector3.Lerp(
                    originalScale * 1.3f, 
                    originalScale, 
                    elapsed / 0.3f
                );
                elapsed += Time.deltaTime;
                yield return null;
            }
            
            heartImage.transform.localScale = originalScale;
        }
    }

    public int GetRemainingExtraLives()
    {
        return currentExtraLives;
    }

    public void AddExtraLives(int amount)
    {
        currentExtraLives += amount;
    }

    public void ResetExtraLives()
    {
        currentExtraLives = extraLivesAvailable;
    }

    public bool CanUseExtraLife()
    {
        return currentExtraLives > 0 && 
               GameManager.Instance.lives < GameManager.Instance.maxLives && 
               !isHealing;
    }
}
