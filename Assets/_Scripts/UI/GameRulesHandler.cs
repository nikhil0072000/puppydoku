using System;
using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// The rules bar under the Game-scene HUD: three tabs, one per placement rule
/// (one per colour / one per row+column / no touching).
///
/// Expected tab hierarchy, wired per tab in the Inspector:
///   Rule (GameObject)
///     ├── Image   — rule icon
///     ├── Label   — TextMeshProUGUI, filled from this component's rule text
///     └── Outline — Image frame, inactive when idle
///
/// When the player makes a mistake, <see cref="GameSceneUI"/> forwards the broken
/// rule here and its outline blinks red to show which rule was missed.
/// </summary>
public class GameRulesHandler : MonoBehaviour
{
    [Serializable]
    private class RuleTab
    {
        [Tooltip("Rule description shown on the tab's label.")]
        [TextArea]
        public string ruleText;

        [Tooltip("The tab's label (Label child). Filled with Rule Text on Awake.")]
        public TextMeshProUGUI label;

        [Tooltip("The tab's outline frame (Outline child). Kept inactive when idle; blinked red on a violation.")]
        public Image outline;

        [NonSerialized] public Sequence activeBlink;
    }

    [Header("Rule Tabs")]
    [SerializeField] private RuleTab onePerColorRule = new RuleTab { ruleText = "1 Puppy per color" };
    [SerializeField] private RuleTab onePerRowAndColumnRule = new RuleTab { ruleText = "1 Puppy per column and row" };
    [SerializeField] private RuleTab cannotTouchRule = new RuleTab { ruleText = "Puppies cannot touch" };

    [Header("Violation Blink")]
    [Tooltip("Outline colour while blinking.")]
    [SerializeField] private Color blinkColor = new Color32(230, 60, 60, 255);
    [Tooltip("How many on/off blinks one violation plays.")]
    [Min(1)]
    [SerializeField] private int blinkCount = 3;
    [Tooltip("Seconds for one fade (half a blink cycle).")]
    [Min(0.02f)]
    [SerializeField] private float blinkFadeDuration = 0.15f;

    private void Awake()
    {
        SetupTab(onePerColorRule);
        SetupTab(onePerRowAndColumnRule);
        SetupTab(cannotTouchRule);
    }

    private void SetupTab(RuleTab tab)
    {
        if (tab.label != null)
            tab.label.text = tab.ruleText;

        if (tab.outline != null)
            tab.outline.gameObject.SetActive(false);
    }

    /// <summary>
    /// Blinks the outline of the tab matching the broken rule.
    /// <see cref="GameRuleType.None"/> is ignored (e.g. a hidden-solution miss
    /// that didn't break any visible rule).
    /// </summary>
    public void FlashViolatedRule(GameRuleType rule)
    {
        RuleTab tab = GetTab(rule);
        if (tab == null || tab.outline == null)
            return;

        // Restart cleanly if this rule is already blinking.
        tab.activeBlink?.Kill();

        Image outline = tab.outline;
        outline.gameObject.SetActive(true);

        Color transparent = blinkColor;
        transparent.a = 0f;
        outline.color = transparent;

        Sequence blink = DOTween.Sequence();
        blink.SetLink(outline.gameObject, LinkBehaviour.KillOnDestroy);
        for (int i = 0; i < blinkCount; i++)
        {
            blink.Append(outline.DOFade(blinkColor.a, blinkFadeDuration).SetEase(Ease.OutQuad))
                 .Append(outline.DOFade(0f, blinkFadeDuration).SetEase(Ease.InQuad));
        }
        blink.OnComplete(() => outline.gameObject.SetActive(false));

        tab.activeBlink = blink;
    }

    private RuleTab GetTab(GameRuleType rule)
    {
        switch (rule)
        {
            case GameRuleType.OnePerColor: return onePerColorRule;
            case GameRuleType.OnePerRowAndColumn: return onePerRowAndColumnRule;
            case GameRuleType.CannotTouch: return cannotTouchRule;
            default: return null;
        }
    }

    private void OnDestroy()
    {
        onePerColorRule.activeBlink?.Kill();
        onePerRowAndColumnRule.activeBlink?.Kill();
        cannotTouchRule.activeBlink?.Kill();
    }
}
