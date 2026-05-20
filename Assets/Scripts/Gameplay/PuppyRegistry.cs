using System.Collections.Generic;

/// <summary>
/// Tracks every live PuzzleObject so the game can broadcast reactions
/// (e.g. play Sad on every revealed puppy when the player makes a wrong move)
/// without scanning the scene.
///
/// Puppies register themselves in OnEnable and unregister in OnDisable, so
/// the list stays in sync across pooling, level reloads, and scene changes.
/// </summary>
public static class PuppyRegistry
{
    private static readonly List<PuzzleObject> _all = new List<PuzzleObject>(32);

    public static IReadOnlyList<PuzzleObject> All => _all;

    public static void Register(PuzzleObject puppy)
    {
        if (puppy == null) return;
        if (!_all.Contains(puppy)) _all.Add(puppy);
    }

    public static void Unregister(PuzzleObject puppy)
    {
        if (puppy == null) return;
        _all.Remove(puppy);
    }

    /// <summary>
    /// Tells every registered puppy to play its Sad reaction. Iterates in
    /// reverse so a puppy destroying itself mid-iteration cannot shift live
    /// indices.
    /// </summary>
    public static void PlaySadOnAll()
    {
        for (int i = _all.Count - 1; i >= 0; i--)
        {
            PuzzleObject pup = _all[i];
            if (pup != null) pup.PlaySad();
        }
    }
}
