using UnityEngine;

public class PuzzleObject : MonoBehaviour
{
    public Vector2Int gridPosition { get; private set; }

    private PuppyAnimator _puppyAnimator;

    private void Awake()
    {
        TryGetComponent(out _puppyAnimator);
    }

    private void OnEnable()
    {
        PuppyRegistry.Register(this);
    }

    private void OnDisable()
    {
        PuppyRegistry.Unregister(this);
    }

    public void Init(Vector2Int pos)
    {
        gridPosition = pos;
        name = $"PuzzleObject_{pos.x}_{pos.y}";
    }

    public void PlayWink()
    {
        if (_puppyAnimator != null) _puppyAnimator.PlayWink();
    }

    public void PlaySad()
    {
        if (_puppyAnimator != null) _puppyAnimator.PlaySad();
    }
}
