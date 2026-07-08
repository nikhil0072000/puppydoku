namespace PuppyPuzzle.Boosters
{
    /// <summary>
    /// Bulb-hint booster button. Purely the button's identity and visual state
    /// (badges, lock, count — driven by BoosterUIController through the base class).
    /// The hint flow itself lives in <see cref="BoosterController"/>.
    /// </summary>
    public class HintBooster : Booster
    {
        public override BoosterType Type => BoosterType.Hint;
    }
}
