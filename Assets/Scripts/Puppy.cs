using UnityEngine;

public class Puppy : MonoBehaviour
{
    public Vector2Int gridPosition { get; private set; }

    public void Init(Vector2Int pos)
    {
        gridPosition = pos;
        name = $"Puppy_{pos.x}_{pos.y}";
    }
}
