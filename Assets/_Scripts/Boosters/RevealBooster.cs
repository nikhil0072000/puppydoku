namespace PuppyPuzzle.Boosters
{
    /// <summary>
    /// Reveal booster button. Purely the button's identity and visual state
    /// (badges, lock, count — driven by BoosterUIController through the base class).
    /// The reveal effect itself lives in <see cref="BoosterController"/>.
    /// </summary>
    public class RevealBooster : Booster
    {
        public override BoosterType Type => BoosterType.Reveal;
    }
}
