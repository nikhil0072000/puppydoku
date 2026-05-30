// Copyright (C) 2017 gamevanilla. All rights reserved.
// This code can only be used under the standard Unity Asset Store End User License Agreement,
// a copy of which is available at http://unity3d.com/company/legal/as_terms.

using UnityEngine;
using UnityEngine.UI;

namespace GameVanilla.Game.UI
{
    /// <summary>
    /// Utility class to set the canvas scaler's match to a value defined in the editor.
    /// </summary>
    public class AdjustCanvasToResolution : MonoBehaviour
    {
        [SerializeField]
        [Range(0f, 1f)]
        [Tooltip("Value applied to the CanvasScaler's Match (Width/Height) on start.")]
        private float canvasScalingMatch = 0.5f;

        /// <summary>
        /// The associated canvas scaler.
        /// </summary>
        private CanvasScaler canvasScaler;

        /// <summary>
        /// Unity's Awake method.
        /// </summary>
        private void Awake()
        {
            canvasScaler = GetComponent<CanvasScaler>();
        }

        /// <summary>
        /// Unity's Start method.
        /// </summary>
        private void Start()
        {
            if (canvasScaler != null)
            {
                canvasScaler.matchWidthOrHeight = canvasScalingMatch;
            }
        }
    }
}
