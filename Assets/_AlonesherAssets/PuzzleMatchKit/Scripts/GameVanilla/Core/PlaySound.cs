// Copyright (C) 2017 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;

namespace GameVanilla.Core
{
    /// <summary>
    /// Utility class to play a sound via the SoundManager.
    /// </summary>
    public class PlaySound : MonoBehaviour
    {
        /// <summary>
        /// Plays the specified sound. The original gamevanilla SoundManager was not imported
        /// with this prefab, so this is currently a no-op. Hook it up to the project's audio
        /// system here if sound playback is needed.
        /// </summary>
        /// <param name="soundName">The name of the sound to play.</param>
        public void Play(string soundName)
        {
        }
    }
}
