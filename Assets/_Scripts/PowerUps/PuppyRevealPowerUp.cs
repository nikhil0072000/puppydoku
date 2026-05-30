using UnityEngine;

namespace PuppyPuzzle.PowerUps
{
    /// <summary>
    /// Puppy power-up: auto-places one guaranteed-correct puppy by asking the
    /// GameManager to solve the board from its current state and reveal a cell.
    /// </summary>
    public class PuppyRevealPowerUp : PowerUpBase
    {
        public override PowerUpType Type => PowerUpType.PuppyReveal;

        public override bool TryActivate()
        {
            if (GameManager.Instance == null)
            {
                Debug.LogWarning("[PuppyRevealPowerUp] No GameManager in scene.");
                return false;
            }

            // Returns false if the board is finished or can't be solved from here —
            // the caller then refunds the spent chance.
            return GameManager.Instance.RevealCorrectPuppy();
        }
    }
}
