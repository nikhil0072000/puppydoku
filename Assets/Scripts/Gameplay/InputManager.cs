using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

public class InputManager : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float doubleTapThreshold = 0.3f;

    // One pending-tap entry per cell, shared by mouse and touch.
    private readonly Dictionary<Cell, Coroutine> pendingTaps = new();

    private Camera mainCam;
    private WaitForSeconds doubleTapWait;

    void Awake()
    {
        mainCam = Camera.main;
        if (mainCam == null)
            Debug.LogError("InputManager: No Main Camera found!");

        doubleTapWait = new WaitForSeconds(doubleTapThreshold);
    }

    void OnEnable()
    {
        EnhancedTouchSupport.Enable();
        Touch.onFingerDown += OnFingerDown;
    }

    void OnDisable()
    {
        Touch.onFingerDown -= OnFingerDown;
        EnhancedTouchSupport.Disable();
    }

    void Update()
    {
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            ProcessPointer(mousePos);
        }
    }

    // ---------- Touch ----------
    private void OnFingerDown(Finger finger)
    {
        ProcessPointer(finger.screenPosition);
    }

    // ---------- Core pointer logic (works for both mouse and touch) ----------
    private void ProcessPointer(Vector2 screenPosition)
    {
        if (GameManager.LevelComplete || GameManager.LevelFailed)
            return;

        Ray ray = mainCam.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        if (hit.collider == null) return;

        Cell cell = hit.collider.GetComponent<Cell>();
        if (cell == null) return;

        // Ignore cells that already have a puppy or are locked by a red cross.
        if (cell.GetPuppy() != null || cell.IsErrorLocked)
        {
            Debug.Log($"Input ignored: Cell {cell.gridPosition} is occupied or locked.");
            return;
        }

        if (pendingTaps.TryGetValue(cell, out Coroutine pending))
        {
            // Second tap on the same cell within threshold -> double tap.
            StopCoroutine(pending);
            pendingTaps.Remove(cell);

            Debug.Log($"Double tap on {cell.gridPosition} -> attempting placement.");
            cell.OnDoubleTap();
        }
        else
        {
            // First tap -> wait for a possible second tap before treating it as a single tap.
            pendingTaps[cell] = StartCoroutine(SingleTapDelay(cell));
            Debug.Log($"First tap on {cell.gridPosition} -> waiting for double tap...");
        }
    }

    private IEnumerator SingleTapDelay(Cell cell)
    {
        yield return doubleTapWait;

        if (pendingTaps.Remove(cell))
        {
            cell.ToggleXMark();
            Debug.Log($"Single tap on {cell.gridPosition} -> X toggled.");
        }
    }
}
