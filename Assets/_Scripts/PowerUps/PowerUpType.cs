namespace PuppyPuzzle.PowerUps
{
    /// <summary>
    /// The consumable power-ups the player can trigger from the bottom bar.
    /// Used as the key for PlayerPrefs chance storage, so DO NOT rename or
    /// reorder existing entries — the enum's string name is persisted.
    /// </summary>
    public enum PowerUpType
    {
        /// <summary>Auto-places one guaranteed-correct puppy.</summary>
        PuppyReveal,

        /// <summary>Guided-deduction hint: previews/commits X marks around a puppy.</summary>
        Hint
    }
}
