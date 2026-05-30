using DG.Tweening;
using UnityEngine;

/// <summary>
/// One-time DOTween runtime configuration. Sets the global tween/sequence
/// capacity high enough that the grid intro animation, tap responses, and the
/// invalid-placement sequence can run without DOTween having to dynamically
/// grow its pool (which logs a warning and re-allocates).
/// </summary>
internal static class DOTweenBootstrap
{
    // Worst-case headroom for: ~81 cell intro tweens + concurrent cell taps +
    // error sequence (9 nested tweens) + heart-break (3 tweens) + a buffer.
    private const int TweenCapacity = 500;
    private const int SequenceCapacity = 100;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void Initialize()
    {
        DOTween.Init(recycleAllByDefault: true, useSafeMode: true, logBehaviour: LogBehaviour.Default)
               .SetCapacity(TweenCapacity, SequenceCapacity);
    }
}
