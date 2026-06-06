using System;

/// <summary>
/// A daily challenge level: a normal level payload plus its sequence number and
/// the PawGem reward granted on completion. Lives in DailyChallengeLevels.json.
/// </summary>
[Serializable]
public class DailyChallengeLevelData : LevelDataModel
{
    /// <summary>1-based sequence number shown in the Daily Challenge panel.</summary>
    public int dailyChallengeLevelNumber = 1;

    /// <summary>PawGems granted when the player completes this daily level.</summary>
    public int gemReward = 0;
}

/// <summary>JSON container for Levels.json: { "levels": [ ... ] }.</summary>
[Serializable]
public class LevelCollection
{
    public LevelDataModel[] levels;
}

/// <summary>JSON container for DailyChallengeLevels.json: { "levels": [ ... ] }.</summary>
[Serializable]
public class DailyChallengeLevelCollection
{
    public DailyChallengeLevelData[] levels;
}
