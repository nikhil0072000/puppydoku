using UnityEngine;

public class PuzzleObject : MonoBehaviour
{
    public Vector2Int gridPosition { get; private set; }

    public void Init(Vector2Int pos)
    {
        gridPosition = pos;
        name = $"PuzzleObject_{pos.x}_{pos.y}";
    }
}
