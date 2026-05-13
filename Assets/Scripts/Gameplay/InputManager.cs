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
    [SerializeField] private float redCrossDuration = 0.5f;    // how long red cross stays

    // --- Touch state ---
    private Dictionary<Cell, Coroutine> touchTapCoroutines = new Dictionary<Cell, Coroutine>();

    // --- Mouse state ---
    private Cell mouseLastClickedCell = null;
    private float mouseLastClickTime = -10f;
    private Coroutine mouseSingleClickCoroutine = null;

    private Camera mainCam;

    void Awake()
    {
        mainCam = Camera.main;
        if (mainCam == null)
            Debug.LogError("InputManager: No Main Camera found!");
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
        // Mouse input polling
        if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePos = Mouse.current.position.ReadValue();
            ProcessMouseClick(mousePos);
        }
    }

    // ================= TOUCH =================
    private void OnFingerDown(Finger finger)
    {
        Ray ray = mainCam.ScreenPointToRay(finger.screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);
        if (hit.collider == null) return;

        Cell cell = hit.collider.GetComponent<Cell>();
        if (cell == null) return;

        ProcessInput(cell, isTouch: true);
    }

    // ================= MOUSE =================
    private void ProcessMouseClick(Vector2 screenPosition)
    {
        Ray ray = mainCam.ScreenPointToRay(screenPosition);
        RaycastHit2D hit = Physics2D.Raycast(ray.origin, ray.direction);

        // If we clicked on nothing or a non‑cell, reset mouse memory
        if (hit.collider == null)
        {
            ResetMouseState();
            return;
        }

        Cell cell = hit.collider.GetComponent<Cell>();
        if (cell == null)
        {
            ResetMouseState();
            return;
        }

        ProcessInput(cell, isTouch: false);
    }

    // ================= COMMON LOGIC =================
    private void ProcessInput(Cell cell, bool isTouch)
    {
        // Ignore if there is already a puppy
        if (cell.GetPuppy() != null)
        {
            Debug.Log($"{InputType(isTouch)}: Cell {cell.gridPosition} has puppy, ignoring.");
            return;
        }

        // Ignore if cell is locked by red cross
        if (cell.IsErrorLocked)
        {
            Debug.Log($"{InputType(isTouch)}: Cell {cell.gridPosition} is locked (red cross), ignoring.");
            return;
        }

        // ---- CASE 1: Cell already has a white X --> attempt placement immediately ----
        if (cell.IsXMarked)
        {
            // Cancel any pending single‑tap coroutine for this cell (if any)
            if (isTouch && touchTapCoroutines.ContainsKey(cell))
            {
                StopCoroutine(touchTapCoroutines[cell]);
                touchTapCoroutines.Remove(cell);
            }
            if (!isTouch && mouseLastClickedCell == cell)
            {
                ResetMouseState();
            }

            Debug.Log($"{InputType(isTouch)}: Cell {cell.gridPosition} is marked – attempting placement.");
            AttemptPlacement(cell);
            return;
        }

        // ---- CASE 2: Cell is unmarked – start double‑tap detection ----
        if (isTouch)
        {
            // Check if a single‑tap coroutine is already running for this cell
            if (touchTapCoroutines.ContainsKey(cell))
            {
                // Second tap => double tap
                StopCoroutine(touchTapCoroutines[cell]);
                touchTapCoroutines.Remove(cell);
                Debug.Log($"{InputType(isTouch)}: Double tap on {cell.gridPosition} – attempting placement.");
                AttemptPlacement(cell);
            }
            else
            {
                // First tap – wait for possible second tap
                Coroutine c = StartCoroutine(SingleTapDelay(cell, isTouch));
                touchTapCoroutines[cell] = c;
                Debug.Log($"{InputType(isTouch)}: First tap on {cell.gridPosition} – waiting...");
            }
        }
        else  // mouse
        {
            float timeSinceLast = Time.time - mouseLastClickTime;
            if (cell == mouseLastClickedCell && timeSinceLast < doubleTapThreshold)
            {
                // Double‑click
                if (mouseSingleClickCoroutine != null)
                {
                    StopCoroutine(mouseSingleClickCoroutine);
                    mouseSingleClickCoroutine = null;
                }
                Debug.Log($"{InputType(isTouch)}: Double click on {cell.gridPosition} – attempting placement.");
                AttemptPlacement(cell);
                ResetMouseState();
            }
            else
            {
                // First click – start single‑click delay
                ResetMouseState();   // cancel any previous
                mouseLastClickedCell = cell;
                mouseLastClickTime = Time.time;
                mouseSingleClickCoroutine = StartCoroutine(SingleTapDelay(cell, isTouch));
                Debug.Log($"{InputType(isTouch)}: First click on {cell.gridPosition} – waiting...");
            }
        }
    }

    // ---- Coroutine for single‑tap delay (both touch and mouse) ----
    IEnumerator SingleTapDelay(Cell cell, bool isTouch)
    {
        yield return new WaitForSeconds(doubleTapThreshold);

        if (isTouch)
        {
            if (touchTapCoroutines.ContainsKey(cell))
            {
                // Still waiting -> this is a single tap that wasn't followed by a second tap
                cell.ToggleXMark();    // show white X (or remove it)
                touchTapCoroutines.Remove(cell);
                Debug.Log($"{InputType(isTouch)}: Single tap on {cell.gridPosition} – X toggled.");
            }
        }
        else  // mouse
        {
            if (mouseLastClickedCell == cell)
            {
                // No second click arrived
                cell.ToggleXMark();
                Debug.Log($"{InputType(isTouch)}: Single click on {cell.gridPosition} – X toggled.");
            }
            ResetMouseState();
        }
    }

    // ---- Attempt placement and handle result ----
    private void AttemptPlacement(Cell cell)
    {
        // We call cell's double‑tap event, which GameManager will handle.
        // But we don't know in InputManager whether placement succeeded.
        // So we'll subscribe to a callback from GameManager, or simply let GameManager call back to cell.
        // For now, we fire event, and it's GameManager's job to show red cross on failure.
        // We'll modify GameManager to return success/failure and trigger feedback.
        // However, to keep it decoupled, we can have GameManager raise an event on failure with cell position.
        // Or simpler: GameManager will call cell.ShowRedCross() directly on failure. Let's do that.
        cell.OnDoubleTap();   // GameManager will decide what to do
    }

    // ---- Helper ----
    private string InputType(bool isTouch) => isTouch ? "Touch" : "Mouse";

    private void ResetMouseState()
    {
        if (mouseSingleClickCoroutine != null)
        {
            StopCoroutine(mouseSingleClickCoroutine);
            mouseSingleClickCoroutine = null;
        }
        mouseLastClickedCell = null;
        mouseLastClickTime = -10f;
    }
}
