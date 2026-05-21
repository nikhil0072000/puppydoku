using UnityEngine;

namespace PuppyPuzzle.PowerUps
{
    /// <summary>
    /// Base for a triggerable power-up. <see cref="PowerUpButton"/> consumes a chance,
    /// calls <see cref="TryActivate"/>, and refunds the chance if it returns false
    /// (e.g. nothing could be done). Subclasses live on the scene and reference
    /// gameplay systems directly.
    /// </summary>
    public abstract class PowerUpBase : MonoBehaviour
    {
        public abstract PowerUpType Type { get; }

        /// <summary>Runs the power-up. Returns false if it could not act (caller refunds the chance).</summary>
        public abstract bool TryActivate();
    }
}
