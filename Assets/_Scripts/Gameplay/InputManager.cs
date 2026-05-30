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

    // Caches the Cell behind each collider so the swipe path doesn't GetComponent every
    // pointer-move frame. Entries are re-resolved if a cached cell has been destroyed.
    private readonly Dictionary<Collider2D, Cell> cellByCollider = new();

    private Camera mainCam;
    private WaitForSeconds doubleTapWait;

    [Header("Swipe Input")]
    [Tooltip("Enable swipe-to-mark behavior for both mouse and touch input.")]
    [SerializeField] private bool swipeEnabled = true;
    [Tooltip("Distance in screen pixels the pointer must move before the drag becomes a swipe.")]
    [SerializeField] private float swipeActivationDistance = 18f;

    private bool pointerDown;
    private int activeTouchId = -1;
    private Vector2 pointerDownPosition;
    private bool swipeActive;
    private Cell swipeStartCell;
    private Cell lastSwipedCell;
    private readonly HashSet<Cell> swipeCells = new();
    private readonly List<Cell> swipeCellsOrdered = new();

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
        Touch.onFingerMove += OnFingerMove;
        Touch.onFingerUp += OnFingerUp;
    }

    void OnDisable()
    {
        Touch.onFingerDown -= OnFingerDown;
        Touch.onFingerMove -= OnFingerMove;
        Touch.onFingerUp -= OnFingerUp;
        EnhancedTouchSupport.Disable();
        ClearPendingState();
    }

    /// <summary>
    /// Cancels any in-flight single-tap coroutines and resets pointer/swipe state. Call
    /// when the grid is rebuilt in-scene so pending taps can't fire on destroyed cells.
    /// </summary>
    public void ClearPendingState()
    {
        foreach (Coroutine pending in pendingTaps.Values)
            if (pending != null) StopCoroutine(pending);
        pendingTaps.Clear();
        cellByCollider.Clear();

        pointerDown = false;
        swipeActive = false;
        activeTouchId = -1;
        swipeStartCell = null;
        lastSwipedCell = null;
        swipeCells.Clear();
        swipeCellsOrdered.Clear();
    }

    void Update()
    {
        if (Mouse.current == null)
            return;

        Vector2 mousePos = Mouse.current.position.ReadValue();

        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            ProcessPointerDown(mousePos);
        }
        else if (pointerDown && activeTouchId == -1 && Mouse.current.leftButton.wasReleasedThisFrame)
        {
            ProcessPointerUp(mousePos);
        }
        else if (pointerDown && activeTouchId == -1 && Mouse.current.leftButton.isPressed)
        {
            ProcessPointerMove(mousePos);
        }
    }

    // ---------- Touch ----------
    private void OnFingerDown(Finger finger)
    {
        if (pointerDown)
            return;

        activeTouchId = finger.index;
        ProcessPointerDown(finger.screenPosition);
    }

    private void OnFingerMove(Finger finger)
    {
        if (!pointerDown || finger.index != activeTouchId)
            return;

        ProcessPointerMove(finger.screenPosition);
    }

    private void OnFingerUp(Finger finger)
    {
        if (!pointerDown || finger.index != activeTouchId)
            return;

        ProcessPointerUp(finger.screenPosition);
        activeTouchId = -1;
    }

    // ---------- Core pointer logic (works for both mouse and touch) ----------
    private void ProcessPointerDown(Vector2 screenPosition)
    {
        if (GameManager.LevelComplete || GameManager.LevelFailed)
            return;

        if (GameManager.InputLocked)
            return;

        pointerDown = true;
        pointerDownPosition = screenPosition;
        swipeActive = false;
        swipeStartCell = GetCellAtScreenPosition(screenPosition);
        lastSwipedCell = null;
        swipeCells.Clear();
        swipeCellsOrdered.Clear();

        ProcessPointer(screenPosition);
    }

    private void ProcessPointerMove(Vector2 screenPosition)
    {
        if (!pointerDown || !swipeEnabled || swipeStartCell == null)
            return;

        if (!swipeActive && Vector2.Distance(pointerDownPosition, screenPosition) >= swipeActivationDistance)
            BeginSwipe();

        if (swipeActive)
            AddSwipeCellAtPosition(screenPosition);
    }

    private void ProcessPointerUp(Vector2 screenPosition)
    {
        if (!pointerDown)
            return;

        if (swipeActive)
        {
            AddSwipeCellAtPosition(screenPosition);
            ApplySwipe();
        }

        pointerDown = false;
        swipeActive = false;
        lastSwipedCell = null;
        swipeCells.Clear();
        swipeCellsOrdered.Clear();
    }

    private void BeginSwipe()
    {
        swipeActive = true;
        CancelPendingTap(swipeStartCell);
        AddSwipeCellAtPosition(pointerDownPosition);
    }

    private void AddSwipeCellAtPosition(Vector2 screenPosition)
    {
        Cell cell = GetCellAtScreenPosition(screenPosition);
        if (cell == null || cell == lastSwipedCell)
            return;

        if (cell.GetPuppy() != null || cell.IsErrorLocked)
            return;

        if (swipeCells.Add(cell))
            swipeCellsOrdered.Add(cell);

        lastSwipedCell = cell;
    }

    private Cell GetCellAtScreenPosition(Vector2 screenPosition)
    {
        if (mainCam == null)
            return null;

        Ray ray = mainCam.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        Collider2D col = hit.collider;
        if (col == null)
            return null;

        if (cellByCollider.TryGetValue(col, out Cell cached) && cached != null)
            return cached;

        Cell cell = col.GetComponent<Cell>();
        if (cell != null)
            cellByCollider[col] = cell;
        return cell;
    }

    private void CancelPendingTap(Cell cell)
    {
        if (cell == null)
            return;

        if (pendingTaps.TryGetValue(cell, out Coroutine pending))
        {
            StopCoroutine(pending);
            pendingTaps.Remove(cell);
        }
    }

    private void ApplySwipe()
    {
        foreach (Cell cell in swipeCellsOrdered)
        {
            if (cell == null)
                continue;

            cell.ToggleXMark();
        }
    }

    private void ProcessPointer(Vector2 screenPosition)
    {
        if (GameManager.LevelComplete || GameManager.LevelFailed)
            return;

        // Suppress grid taps while a power-up overlay (e.g. the Bulb hint) is open.
        if (GameManager.InputLocked)
            return;

        Cell cell = GetCellAtScreenPosition(screenPosition);
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
            // The cell may have been destroyed by an in-scene grid rebuild during the wait.
            if (cell != null)
            {
                cell.ToggleXMark();
                Debug.Log($"Single tap on {cell.gridPosition} -> X toggled.");
            }
        }
    }
}
