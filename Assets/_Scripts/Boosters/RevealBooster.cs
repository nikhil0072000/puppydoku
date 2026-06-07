using UnityEngine;

namespace PuppyPuzzle.Boosters
{
    /// <summary>
    /// Reveal booster: auto-places one guaranteed-correct puppy by asking the
    /// GameManager to solve the board from its current state and reveal a cell.
    /// </summary>
    public class RevealBooster : Booster
    {
        public override BoosterType Type => BoosterType.Reveal;

        public override bool TryActivate()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[RevealBooster] No GameManager in scene.");
                return false;
            }

            // Returns false if the board is finished or can't be solved from here —
            // the caller then refunds the spent cost.
            return GameManager.Instance.RevealCorrectPuppy();
        }
    }
}
