using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

/// <summary>
/// Forces Play mode to always start from the Loading scene, no matter which scene
/// is open in the editor — so the LevelLoader boot flow always runs. The scene you
/// were editing is restored automatically when you exit Play mode.
///
/// Toggle via "Tools/Play From Loading Scene" (on by default). The setting uses
/// EditorSceneManager.playModeStartScene, which is session-only, so it is
/// re-applied on every editor load / domain reload.
/// </summary>
[InitializeOnLoad]
public static class PlayFromLoadingScene
{
    private const string LoadingScenePath = "Assets/_Scenes/Loading.unity";
    private const string MenuPath = "Tools/Play From Loading Scene";
    private const string PrefKey = "PuppyDoku.PlayFromLoadingScene";

    static PlayFromLoadingScene()
    {
        // Delay so the AssetDatabase is ready when the editor is still booting.
        EditorApplication.delayCall += Apply;
    }

    private static bool Enabled
    {
        get => EditorPrefs.GetBool(PrefKey, true);
        set => EditorPrefs.SetBool(PrefKey, value);
    }

    [MenuItem(MenuPath)]
    private static void Toggle()
    {
        Enabled = !Enabled;
        Apply();
        Debug.Log($"[PlayFromLoadingScene] Play mode now starts from " +
                  $"{(Enabled ? "the Loading scene" : "the currently open scene")}.");
    }

    [MenuItem(MenuPath, true)]
    private static bool ToggleValidate()
    {
        Menu.SetChecked(MenuPath, Enabled);
        return true;
    }

    private static void Apply()
    {
        if (!Enabled)
        {
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        SceneAsset loadingScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(LoadingScenePath);
        if (loadingScene == null)
        {
            Debug.LogWarning($"[PlayFromLoadingScene] Loading scene not found at '{LoadingScenePath}' — " +
                             "Play mode will start from the open scene.");
            EditorSceneManager.playModeStartScene = null;
            return;
        }

        EditorSceneManager.playModeStartScene = loadingScene;
    }
}
