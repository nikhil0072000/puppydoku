/// <summary>
/// The three placement rules shown on the Game-scene rules bar. Produced by
/// <see cref="GameRules.GetFirstViolation"/> so the UI can blink the rule a
/// wrong move broke.
/// </summary>
public enum GameRuleType
{
    /// <summary>No rule broken (legal placement).</summary>
    None = 0,

    /// <summary>Only one puppy allowed per colour zone.</summary>
    OnePerColor = 1,

    /// <summary>Only one puppy allowed per row and per column.</summary>
    OnePerRowAndColumn = 2,

    /// <summary>Puppies cannot touch, not even diagonally.</summary>
    CannotTouch = 3,
}
