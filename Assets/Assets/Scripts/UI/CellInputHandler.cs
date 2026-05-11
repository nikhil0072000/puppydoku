using UnityEngine;
using UnityEngine.EventSystems;

public class CellInputHandler : MonoBehaviour, IPointerClickHandler
{
    float lastClickTime = 0;
    float doubleClickThreshold = 0.4f;
    Cell cell;

    void Awake() => cell = GetComponent<Cell>();

    public void OnPointerClick(PointerEventData eventData)
    {
        if (Time.time - lastClickTime < doubleClickThreshold)
        {
            // Double click detected
            GameManager.Instance.OnCellTapped(cell.position);
            lastClickTime = 0; // reset to avoid triple
        }
        else
        {
            lastClickTime = Time.time;
        }
    }
}
