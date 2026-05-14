using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;

public class LevelValidatorWindow : EditorWindow
{
    private Vector2 scrollPos;
    private string report = "";

    [MenuItem("Window/PuppyPuzzle/Level Validator")]
    public static void ShowWindow()
    {
        GetWindow<LevelValidatorWindow>("Level Validator");
    }

    void OnGUI()
    {
        EditorGUILayout.LabelField("Level Validator", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        if (GUILayout.Button("Validate All Levels", GUILayout.Height(30)))
        {
            ValidateAllLevels();
        }

        EditorGUILayout.Space();
        EditorGUILayout.LabelField("Report:", EditorStyles.boldLabel);
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos, GUILayout.Height(300));
        EditorGUILayout.TextArea(report, GUILayout.ExpandHeight(true));
        EditorGUILayout.EndScrollView();
    }

    void ValidateAllLevels()
    {
        report = "";
        string folder = Path.Combine(Application.dataPath, "StreamingAssets", "Levels");
        if (!Directory.Exists(folder))
        {
            report = "Levels folder not found.";
            return;
        }

        string[] files = Directory.GetFiles(folder, "*.json");
        if (files.Length == 0)
        {
            report = "No JSON level files found.";
            return;
        }

        int errorCount = 0;
        foreach (string filePath in files)
        {
            string fileName = Path.GetFileName(filePath);
            string json = File.ReadAllText(filePath);
            LevelDataModel model = null;

            try
            {
                model = JsonConvert.DeserializeObject<LevelDataModel>(json);
            }
            catch (System.Exception e)
            {
                report += $"<color=red>ERROR:</color> {fileName} – Invalid JSON: {e.Message}\n";
                errorCount++;
                continue;
            }

            if (model == null)
            {
                report += $"<color=red>ERROR:</color> {fileName} – Null data.\n";
                errorCount++;
                continue;
            }

            List<string> errors = new List<string>();
            int size = model.GetSafeGridSize();
            if (size < 4) errors.Add($"Grid size {size} below minimum (4).");
            if (string.IsNullOrWhiteSpace(model.levelId)) errors.Add("Missing Level ID.");
            if (string.IsNullOrWhiteSpace(model.difficulty)) errors.Add("Missing difficulty.");

            int[,] map = model.GetSafeColorData();
            if (map.GetLength(0) != size || map.GetLength(1) != size)
                errors.Add("Color data dimensions mismatch.");

            HashSet<int> unique = new HashSet<int>();
            for (int y = 0; y < size; y++)
                for (int x = 0; x < size; x++)
                {
                    if (map[x, y] < 0) errors.Add($"Negative color ID at ({x},{y}).");
                    unique.Add(map[x, y]);
                }
            if (unique.Count == 0) errors.Add("No colours on grid.");

            int winTarget = model.GetSafeWinCondition();
            if (winTarget != unique.Count)
                errors.Add($"Win condition {winTarget} doesn't match unique colours {unique.Count}.");
            if (winTarget > size)
                errors.Add($"Win condition {winTarget} exceeds grid size {size}.");

            Vector2Int[] pre = model.GetSafePrePlaced();
            foreach (Vector2Int pos in pre)
            {
                if (pos.x < 0 || pos.x >= size || pos.y < 0 || pos.y >= size)
                    errors.Add($"Pre-placed {pos} out of bounds.");
            }

            HashSet<Vector2Int> preSet = new HashSet<Vector2Int>(pre);
            if (preSet.Count != pre.Length)
                errors.Add("Duplicate pre-placed positions detected.");

            Vector2Int[] preArray = new Vector2Int[preSet.Count];
            preSet.CopyTo(preArray);
            for (int i = 0; i < preArray.Length; i++)
            {
                for (int j = i + 1; j < preArray.Length; j++)
                {
                    Vector2Int pos = preArray[i];
                    Vector2Int other = preArray[j];
                    if (pos.x == other.x)
                        errors.Add($"Pre-placed conflict: same row {pos} and {other}.");
                    if (pos.y == other.y)
                        errors.Add($"Pre-placed conflict: same column {pos} and {other}.");
                    if (Mathf.Abs(pos.x - other.x) == 1 && Mathf.Abs(pos.y - other.y) == 1)
                        errors.Add($"Pre-placed conflict: diagonal {pos} and {other}.");
                }
            }

            if (errors.Count > 0)
            {
                errorCount++;
                report += $"<color=red>FAILED:</color> {fileName}\n";
                foreach (string err in errors)
                    report += $"    - {err}\n";
            }
            else
            {
                report += $"<color=green>OK:</color> {fileName} (grid: {size}x{size}, colours: {unique.Count})\n";
            }
        }

        report += $"\nTotal files: {files.Length}, Errors: {errorCount}\n";
        if (errorCount == 0) report += "<color=green>All levels valid!</color>";
        Repaint();
    }
}
