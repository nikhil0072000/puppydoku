using UnityEngine;

public class CatAnimator : MonoBehaviour
{
    [Header("Animation Settings")]
    public float idleBounceSpeed = 2f;
    public float idleBounceAmount = 0.05f;
    public float placementBounceScale = 1.2f;
    public float placementBounceDuration = 0.3f;
    
    private Animator animator;
    private Vector3 originalScale;
    private bool isIdleAnimating = true;

    void Start()
    {
        animator = GetComponent<Animator>();
        originalScale = transform.localScale;
        
        // Start idle animation
        StartCoroutine(IdleAnimation());
    }

    System.Collections.IEnumerator IdleAnimation()
    {
        while (isIdleAnimating)
        {
            float bounce = Mathf.Sin(Time.time * idleBounceSpeed) * idleBounceAmount;
            transform.localScale = originalScale + Vector3.one * bounce;
            yield return null;
        }
    }

    public void PlayPlacementAnimation()
    {
        StartCoroutine(PlacementAnimation());
    }

    System.Collections.IEnumerator PlacementAnimation()
    {
        isIdleAnimating = false;
        
        // Bounce up
        float elapsed = 0f;
        while (elapsed < placementBounceDuration / 2f)
        {
            float scale = Mathf.Lerp(1f, placementBounceScale, elapsed / (placementBounceDuration / 2f));
            transform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        // Bounce down
        elapsed = 0f;
        while (elapsed < placementBounceDuration / 2f)
        {
            float scale = Mathf.Lerp(placementBounceScale, 1f, elapsed / (placementBounceDuration / 2f));
            transform.localScale = originalScale * scale;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.localScale = originalScale;
        isIdleAnimating = true;
        StartCoroutine(IdleAnimation());
    }

    public void PlayMistakeAnimation()
    {
        StartCoroutine(MistakeAnimation());
    }

    System.Collections.IEnumerator MistakeAnimation()
    {
        isIdleAnimating = false;
        
        // Shake effect
        Vector3 originalPosition = transform.position;
        float shakeDuration = 0.5f;
        float shakeIntensity = 0.1f;
        
        float elapsed = 0f;
        while (elapsed < shakeDuration)
        {
            Vector3 shakeOffset = Random.insideUnitCircle * shakeIntensity * (1f - elapsed / shakeDuration);
            transform.position = originalPosition + shakeOffset;
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        transform.position = originalPosition;
        isIdleAnimating = true;
        StartCoroutine(IdleAnimation());
    }

    public void StopIdleAnimation()
    {
        isIdleAnimating = false;
        transform.localScale = originalScale;
    }
}
