namespace PuppyPuzzle.Boosters
{
    /// <summary>
    /// The consumable boosters the player can trigger from the bottom bar.
    /// Used as the key for PlayerPrefs count storage (legacy "powerup_chances_" prefix
    /// in EconomyHandler), so DO NOT rename or reorder existing entries — the enum's
    /// string name is persisted.
    /// </summary>
    public enum BoosterType
    {
        /// <summary>Auto-places one guaranteed-correct puppy.</summary>
        Reveal,

        /// <summary>Guided-deduction hint: previews/commits X marks around a puppy.</summary>
        Hint
    }
}
