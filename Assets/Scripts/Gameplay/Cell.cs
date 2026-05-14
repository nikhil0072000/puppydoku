using UnityEngine;
using System;
using System.Collections;

public class Cell : MonoBehaviour
{
    // No more static ZoneColors array here – colour comes from outside.
    public static event Action<Vector2Int> OnCellDoubleTapped;

    [SerializeField] private SpriteRenderer zoneOverlay;
    [SerializeField] private SpriteRenderer xMarkRenderer;

    public Vector2Int gridPosition;
    public int zoneID;
    public bool IsXMarked;
    public bool IsErrorLocked;
    public bool isGiven;       // <-- NEW

    private PuzzleObject currentPuppy;
    private Color originalZoneColor;

    
    public void Init(int x, int y, int zone, Color zoneColor)   // <-- zoneColor parameter
    {
        gridPosition = new Vector2Int(x, y);
        zoneID = zone;
        originalZoneColor = zoneColor;   // <-- SAVE COLOR
        isGiven = false;                     // given state set separately
        IsErrorLocked = false;
        IsXMarked = false;

        if (zoneOverlay != null)
            zoneOverlay.color = zoneColor;   // directly use the passed colour
        else
            Debug.LogWarning($"Cell ({x},{y}): zoneOverlay missing.");

        if (xMarkRenderer != null)
        {
            xMarkRenderer.gameObject.SetActive(false);
            xMarkRenderer.color = Color.white;   // default white X
        }
        else
            Debug.LogWarning($"Cell ({x},{y}): xMarkRenderer missing.");

        gameObject.name = $"Cell_{x}_{y}_Zone{zone}";
    }

    // ---- Single tap: toggle white X ----
    public void ToggleXMark()
    {
        // Can't mark a cell that already has a puppy
        if (currentPuppy != null)
        {
            Debug.Log($"Cell {gridPosition}: already has puppy, X mark not allowed.");
            return;
        }

        if (IsErrorLocked)
        {
            Debug.Log($"Cell {gridPosition}: locked, cannot toggle X.");
            return;
        }

        IsXMarked = !IsXMarked;
        if (xMarkRenderer != null)
        {
            xMarkRenderer.gameObject.SetActive(IsXMarked);
            xMarkRenderer.color = Color.white;   // always white for toggleable mark
        }
        Debug.Log($"Cell {gridPosition}: white X mark toggled to {IsXMarked}");
    }

    // ---- Show permanent red cross (error feedback) ----
    public void ShowPermanentRedCross()
    {
        if (xMarkRenderer == null) return;

        // Set the X mark to red and ensure it's enabled
        xMarkRenderer.color = new Color32(255, 73, 0, 255); // FF4900
        xMarkRenderer.gameObject.SetActive(true);
        IsXMarked = false;
        IsErrorLocked = true;   // lock the cell from further input

        Debug.Log($"Cell {gridPosition}: permanent red cross, input locked.");
    }

    // ---- Double tap: raise event to attempt placement ----
    public void OnDoubleTap()
    {
        if (currentPuppy != null)
        {
            Debug.Log($"Cell {gridPosition}: puppy already present, double tap ignored.");
            return;
        }

        if (IsErrorLocked)
        {
            Debug.Log($"Cell {gridPosition}: locked, cannot place.");
            return;
        }

        Debug.Log($"Cell {gridPosition}: requesting puppy placement.");
        OnCellDoubleTapped?.Invoke(gridPosition);
    }

    // ---- General placement attempt (called from InputManager) ----
    public void AttemptPlacement()
    {
        // This simply triggers to same double‑tap flow
        OnDoubleTap();
    }

    // ---- Place a puppy (called by GameManager) ----
    public PuzzleObject PlacePuppy(GameObject puppyPrefab)
    {
        if (currentPuppy != null)
        {
            Debug.LogWarning($"Cell {gridPosition}: already has a puppy!");
            return null;
        }

        // Clear any X mark (whether white or red)
        IsXMarked = false;
        if (xMarkRenderer != null)
        {
            xMarkRenderer.gameObject.SetActive(false);
            xMarkRenderer.color = Color.white;   // reset colour
        }

        Vector3 worldPos = transform.position;
        worldPos.z = -0.1f;   // slightly in front of the grid

        GameObject pupObj = Instantiate(puppyPrefab, worldPos, Quaternion.identity, transform);
        PuzzleObject pup = pupObj.GetComponent<PuzzleObject>();
        if (pup == null)
        {
            Debug.LogError("Prefab is missing PuzzleObject script!");
            Destroy(pupObj);
            return null;
        }

        pup.Init(gridPosition);
        currentPuppy = pup;

        // Dim the zone color to show occupation
        if (zoneOverlay != null)
            zoneOverlay.color = Color.Lerp(originalZoneColor, Color.gray, 0.5f);

        return pup;
    }

    public PuzzleObject GetPuppy() => currentPuppy;

    // (Not used yet, but ready for later)
    public void RemovePuppy()
    {
        if (currentPuppy != null)
        {
            Destroy(currentPuppy.gameObject);
            currentPuppy = null;
            if (zoneOverlay != null)
                zoneOverlay.color = originalZoneColor;
        }
    }
}