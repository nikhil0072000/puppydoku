using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class PowerUpButton : MonoBehaviour
{
    [Header("Power-Up Settings")]
    public PowerUpType powerUpType;
    public int maxUses = 3;
    public int currentUses;
    
    [Header("UI Components")]
    public Button button;
    public TextMeshProUGUI countText;
    public Image cooldownOverlay;
    public GameObject disabledOverlay;
    
    [Header("Visual Feedback")]
    public float cooldownDuration = 1f;
    public Color normalColor = Color.white;
    public Color disabledColor = Color.gray;
    public Color activeColor = Color.yellow;

    void Start()
    {
        currentUses = maxUses;
        UpdateUI();
        
        button.onClick.AddListener(OnPowerUpClicked);
    }

    void OnPowerUpClicked()
    {
        if (currentUses <= 0 || IsOnCooldown())
            return;
            
        UsePowerUp();
        StartCooldown();
    }

    void UsePowerUp()
    {
        currentUses--;
        UpdateUI();
        
        switch (powerUpType)
        {
            case PowerUpType.Hint:
                PowerUpManager.Instance.UseHint();
                break;
            case PowerUpType.ExtraLife:
                PowerUpManager.Instance.UseExtraLife();
                break;
            case PowerUpType.RegionCheck:
                PowerUpManager.Instance.UseRegionCheck();
                break;
        }
        
        // Visual feedback
        StartCoroutine(UseEffect());
    }

    System.Collections.IEnumerator UseEffect()
    {
        button.image.color = activeColor;
        yield return new WaitForSeconds(0.2f);
        button.image.color = normalColor;
    }

    void StartCooldown()
    {
        StartCoroutine(CooldownRoutine());
    }

    System.Collections.IEnumerator CooldownRoutine()
    {
        float elapsed = 0f;
        while (elapsed < cooldownDuration)
        {
            float fillAmount = 1f - (elapsed / cooldownDuration);
            cooldownOverlay.fillAmount = fillAmount;
            elapsed += Time.deltaTime;
            yield return null;
        }
        cooldownOverlay.fillAmount = 0f;
    }

    bool IsOnCooldown()
    {
        return cooldownOverlay.fillAmount > 0f;
    }

    void UpdateUI()
    {
        countText.text = currentUses.ToString();
        
        bool isDisabled = currentUses <= 0;
        disabledOverlay.SetActive(isDisabled);
        button.interactable = !isDisabled;
        
        if (isDisabled)
        {
            button.image.color = disabledColor;
        }
        else
        {
            button.image.color = normalColor;
        }
    }

    public void AddUses(int amount)
    {
        currentUses = Mathf.Min(currentUses + amount, maxUses);
        UpdateUI();
    }

    public void ResetUses()
    {
        currentUses = maxUses;
        UpdateUI();
    }
}

public enum PowerUpType
{
    Hint,
    ExtraLife,
    RegionCheck
}
