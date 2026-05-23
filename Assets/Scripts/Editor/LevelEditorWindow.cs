using UnityEngine;
using UnityEditor;
using System.IO;
using System.Collections.Generic;
using Newtonsoft.Json;
using PuppyPuzzle.PowerUps;

public class LevelEditorWindow : EditorWindow
{
    // ---------- Level settings ----------
    private string levelId = "Level_New";
    private LevelType levelType = LevelType.Normal;
    private Difficulty difficulty = Difficulty.Easy;
    private int gridSize = 4;
    private int[,] colorData;   // the drawn zone IDs (ColorID values)

    // ---------- Drawing state ----------
    private int selectedColorId = 0;   // current brush colour (ColorID enum value)
    private bool isPuppyMode = false; // toggle between Paint / Pre-Place
    private HashSet<Vector2Int> prePlacedPositions = new HashSet<Vector2Int>();
    private Vector2Int[] hiddenSolution = null;
    private int winCondition = -1;    // -1 means auto from current grid
    private Vector2 scrollPos;

    // ---------- Color palette (taken from GridConfig at runtime) ----------
    private Color[] paletteColors;     // actual Unity Colors for each ColorID
    private string[] paletteNames;     // names of the ColorID values

    [MenuItem("Window/PuppyPuzzle/Level Editor")]
    public static void ShowWindow()
    {
        GetWindow<LevelEditorWindow>("Level Editor");
    }

    void OnEnable()
    {
        // Default grid
        colorData = new int[gridSize, gridSize];
        LoadPalette();
    }

    void LoadPalette()
    {
        GridConfig config = Resources.Load<GridConfig>("GridConfig");
        if (config == null)
        {
            Debug.LogError("GridConfig not found in Resources!");
            paletteColors = new Color[] { Color.white };
            paletteNames = new string[] { "0" };
            return;
        }

        // We'll get all ColorID values defined in the enum
        var enumValues = System.Enum.GetValues(typeof(ColorID));
        paletteColors = new Color[enumValues.Length];
        paletteNames = new string[enumValues.Length];

        int i = 0;
        foreach (ColorID cid in enumValues)
        {
            paletteColors[i] = config.GetColor(cid);
            paletteNames[i] = cid.ToString();
            i++;
        }
    }

    void OnGUI()
    {
        scrollPos = EditorGUILayout.BeginScrollView(scrollPos);

        EditorGUILayout.LabelField("Level Editor", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // --- Level Info ---
        // Level ID prefix is driven by the type: Level_* / Daily_* / Tutorial_*.
        EditorGUI.BeginChangeCheck();
        levelType = (LevelType)EditorGUILayout.EnumPopup("Level Type", levelType);
        if (EditorGUI.EndChangeCheck())
            levelId = LevelDataModel.ApplyPrefixForType(levelId, levelType);

        levelId = EditorGUILayout.TextField("Level ID", levelId);
        EditorGUILayout.LabelField(" ", $"Saved as: {LevelDataModel.ApplyPrefixForType(levelId, levelType).Replace(" ", "_")}.json", EditorStyles.miniLabel);
        difficulty = (Difficulty)EditorGUILayout.EnumPopup("Difficulty", difficulty);
        int newSize = EditorGUILayout.IntField("Grid Size", gridSize);
        if (newSize != gridSize && newSize >= 2)
        {
            gridSize = newSize;
            colorData = new int[gridSize, gridSize];
            prePlacedPositions.Clear();
            hiddenSolution = null;
        }

        EditorGUILayout.Space();

        // --- Mode toggle ---
        EditorGUILayout.BeginHorizontal();
        EditorGUILayout.LabelField("Mode:", GUILayout.Width(50));
        if (GUILayout.Toggle(!isPuppyMode, "Paint Colors", EditorStyles.miniButtonLeft)) isPuppyMode = false;
        if (GUILayout.Toggle(isPuppyMode, "Place Puppies", EditorStyles.miniButtonRight)) isPuppyMode = true;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        if (!isPuppyMode)
        {
            EditorGUILayout.LabelField("Select Brush Color:", EditorStyles.boldLabel);
            if (paletteColors != null)
            {
                EditorGUILayout.BeginHorizontal();
                for (int i = 0; i < paletteColors.Length; i++)
                {
                    GUI.color = paletteColors[i];
                    if (GUILayout.Button(paletteNames[i], GUILayout.Width(80), GUILayout.Height(30)))
                    {
                        selectedColorId = i;   // assuming palette index matches ColorID enum order
                    }
                }
                GUI.color = Color.white;
                EditorGUILayout.EndHorizontal();
            }
        }
        else
        {
            EditorGUILayout.LabelField("Click a cell to toggle a pre-placed puppy.", EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.Space();

        // --- Grid Drawing ---
        if (colorData == null || colorData.GetLength(0) != gridSize)
            colorData = new int[gridSize, gridSize];

        EditorGUILayout.LabelField("Grid (click to modify):", EditorStyles.boldLabel);
        for (int y = 0; y < gridSize; y++)
        {
            EditorGUILayout.BeginHorizontal();
            for (int x = 0; x < gridSize; x++)
            {
                int cid = colorData[x, y];
                Color cellColor = (cid >= 0 && cid < paletteColors.Length) ? paletteColors[cid] : Color.white;
                GUI.backgroundColor = cellColor;

                string label = prePlacedPositions.Contains(new Vector2Int(x, y)) ? "🐾" : "";
                if (GUILayout.Button(label, GUILayout.Width(30), GUILayout.Height(30)))
                {
                    if (isPuppyMode)
                    {
                        Vector2Int pos = new Vector2Int(x, y);
                        if (prePlacedPositions.Contains(pos))
                            prePlacedPositions.Remove(pos);
                        else
                            prePlacedPositions.Add(pos);
                    }
                    else
                    {
                        colorData[x, y] = selectedColorId;
                    }
                }
            }
            EditorGUILayout.EndHorizontal();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        int uniqueColors = CountUniqueColors();
        int minWin = Mathf.Max(prePlacedPositions.Count, 1);
        int maxWin = Mathf.Max(minWin, Mathf.Min(uniqueColors, gridSize));
        if (winCondition < minWin || winCondition > maxWin)
            winCondition = maxWin;

        EditorGUILayout.LabelField("Win Condition", EditorStyles.boldLabel);
        EditorGUILayout.LabelField($"Colour groups on grid: {uniqueColors}");
        EditorGUILayout.LabelField($"Hidden puppies range: {minWin} .. {maxWin}");
        winCondition = EditorGUILayout.IntSlider("Win Condition", winCondition, minWin, maxWin);

        if (uniqueColors > gridSize)
        {
            EditorGUILayout.HelpBox("Unique colours exceed grid size! Puzzle may be impossible.", MessageType.Warning);
        }

        if (prePlacedPositions.Count > 0)
        {
            string positions = string.Join(", ", System.Linq.Enumerable.Select(prePlacedPositions, pos => $"({pos.x},{pos.y})"));
            EditorGUILayout.LabelField($"Pre-placed puppies: {positions}");
        }

        if (hiddenSolution != null && hiddenSolution.Length > 0)
        {
            string solutionPositions = string.Join(", ", System.Linq.Enumerable.Select(hiddenSolution, pos => $"({pos.x},{pos.y})"));
            EditorGUILayout.LabelField($"Hidden solution stored: {hiddenSolution.Length} positions");
            EditorGUILayout.LabelField(solutionPositions, EditorStyles.wordWrappedLabel);
        }

        EditorGUILayout.Space();

        if (GUILayout.Button("Generate Hidden Solution", GUILayout.Height(30)))
        {
            GenerateHiddenSolution();
        }

        EditorGUILayout.Space();

        // --- Buttons ---
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("Clear All", GUILayout.Height(30)))
        {
            colorData = new int[gridSize, gridSize];
            prePlacedPositions.Clear();
            hiddenSolution = null;
        }
        GUI.backgroundColor = Color.green;
        if (GUILayout.Button("Save to JSON", GUILayout.Height(30)))
        {
            SaveLevelToJson();
        }
        GUI.backgroundColor = Color.white;
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.EndScrollView();
    }

    int CountUniqueColors()
    {
        HashSet<int> unique = new HashSet<int>();
        for (int y = 0; y < gridSize; y++)
            for (int x = 0; x < gridSize; x++)
                unique.Add(colorData[x, y]);
        return unique.Count;
    }

    int GetWinCondition(int uniqueColors)
    {
        int minWin = Mathf.Max(prePlacedPositions.Count, 1);
        int maxWin = Mathf.Max(minWin, Mathf.Min(uniqueColors, gridSize));
        if (winCondition < minWin || winCondition > maxWin)
            return maxWin;
        return winCondition;
    }

    void GenerateHiddenSolution()
    {
        int[,] zoneMap = new int[gridSize, gridSize];
        int maxZone = -1;
        for (int y = 0; y < gridSize; y++)
        {
            for (int x = 0; x < gridSize; x++)
            {
                zoneMap[x, y] = colorData[x, y];
                if (zoneMap[x, y] > maxZone) maxZone = zoneMap[x, y];
            }
        }

        int[] zoneToColorIndex = new int[maxZone + 1];
        for (int i = 0; i <= maxZone; i++)
            zoneToColorIndex[i] = i;

        var alreadyPlaced = new HashSet<Vector2Int>(prePlacedPositions);
        int targetCount = GetWinCondition(CountUniqueColors());
        if (!PuzzleSolver.TrySolve(zoneMap, zoneToColorIndex, CountUniqueColors(), alreadyPlaced, out var solution))
        {
            EditorUtility.DisplayDialog("Hidden Solution", "No valid hidden solution could be found for this puzzle.", "OK");
            hiddenSolution = null;
            return;
        }

        if (solution.Count < targetCount)
        {
            EditorUtility.DisplayDialog("Hidden Solution", "Solver returned fewer positions than the requested win condition.", "OK");
            hiddenSolution = solution.ToArray();
            return;
        }

        hiddenSolution = solution.GetRange(0, targetCount).ToArray();
        EditorUtility.DisplayDialog("Hidden Solution", $"Hidden solution generated with {hiddenSolution.Length} puppies.", "OK");
    }

    PrePlacedData[] ConvertSolutionToData(Vector2Int[] solution)
    {
        if (solution == null) return null;

        var result = new PrePlacedData[solution.Length];
        for (int i = 0; i < solution.Length; i++)
        {
            result[i] = new PrePlacedData { x = solution[i].x, y = solution[i].y };
        }
        return result;
    }

    void SaveLevelToJson()
    {
        int uq = CountUniqueColors();

        // Build LevelDataModel
        int currentWinCondition = GetWinCondition(uq);

        // Enforce the type-correct prefix so the file name and level type always agree.
        levelId = LevelDataModel.ApplyPrefixForType(levelId, levelType);

        LevelDataModel model = new LevelDataModel
        {
            levelId = levelId,
            levelType = levelType.ToString(),
            difficulty = difficulty.ToString(),
            gridSize = gridSize,
            colorData = new int[gridSize][],
            prePlaced = new PrePlacedData[prePlacedPositions.Count],
            solution = hiddenSolution != null ? ConvertSolutionToData(hiddenSolution) : null,
            winCondition = currentWinCondition
        };

        for (int y = 0; y < gridSize; y++)
        {
            model.colorData[y] = new int[gridSize];
            for (int x = 0; x < gridSize; x++)
                model.colorData[y][x] = colorData[x, y];   // row-major
        }

        int idx = 0;
        foreach (Vector2Int pos in prePlacedPositions)
        {
            model.prePlaced[idx++] = new PrePlacedData { x = pos.x, y = pos.y };
        }

        string json = JsonConvert.SerializeObject(model, Formatting.Indented);
        string folder = Path.Combine(Application.dataPath, "StreamingAssets", "Levels");
        if (!Directory.Exists(folder))
            Directory.CreateDirectory(folder);

        string fileName = $"{levelId.Replace(" ", "_")}.json";
        string filePath = Path.Combine(folder, fileName);

        File.WriteAllText(filePath, json);
        AssetDatabase.Refresh();

        Debug.Log($"Level saved to: {filePath}");
        EditorUtility.DisplayDialog("Level Editor", $"Level saved as {fileName}", "OK");
    }
}
