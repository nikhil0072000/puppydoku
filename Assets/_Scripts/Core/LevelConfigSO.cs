using UnityEngine;

/// <summary>
/// Single source for all level content. Assign two JSON TextAssets:
/// - Levels: every Tutorial/Normal level, as { "levels": [ LevelDataModel, ... ] }
/// - DailyChallengeLevels: daily levels with dailyChallengeLevelNumber + gemReward,
///   as { "levels": [ DailyChallengeLevelData, ... ] }
/// Assign the asset to <see cref="LevelLoader"/> in the Loading scene.
/// </summary>
[CreateAssetMenu(fileName = "LevelConfig", menuName = "PuppyPuzzle/Level Config")]
public class LevelConfigSO : ScriptableObject
{
    [Tooltip("JSON containing all Tutorial/Normal levels: { \"levels\": [ ... ] }")]
    [SerializeField] private TextAsset levels;

    [Tooltip("JSON containing all daily challenge levels: { \"levels\": [ ... ] } with dailyChallengeLevelNumber and gemReward per entry.")]
    [SerializeField] private TextAsset dailyChallengeLevels;

    public TextAsset Levels => levels;
    public TextAsset DailyChallengeLevels => dailyChallengeLevels;
}
