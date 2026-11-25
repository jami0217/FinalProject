using System.IO;
using FinalProject.Gym.Squat;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class SquatMinigameMenu
{
    [MenuItem("Tools/Gym/Create Squat UI In Current Scene")]
    private static void CreateInScene()
    {
        SquatMinigameController existing = Object.FindFirstObjectByType<SquatMinigameController>(FindObjectsInactive.Include);
        if (existing != null)
        {
            Selection.activeGameObject = existing.gameObject;
            return;
        }
        SquatMinigameController controller = SquatMinigameUIFactory.Create();
        Undo.RegisterCreatedObjectUndo(controller.gameObject, "Create Squat Minigame UI");
        EditorSceneManager.MarkSceneDirty(controller.gameObject.scene);
        Selection.activeGameObject = controller.gameObject;
    }

    [MenuItem("Tools/Gym/Save New Squat UI Prefab")]
    private static void CreatePrefab()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save new Squat UI prefab", "SquatMinigameUI", "prefab", "Choose a new prefab path; existing assets are preserved.");
        if (string.IsNullOrEmpty(path)) return;
        if (File.Exists(path))
        {
            Debug.LogWarning("Squat UI prefab was not saved: that file already exists. Choose a new filename.");
            return;
        }
        SquatMinigameController controller = SquatMinigameUIFactory.Create();
        try
        {
            PrefabUtility.SaveAsPrefabAsset(controller.gameObject, path);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        finally
        {
            // Only remove the temporary object just created for saving this new prefab.
            Object.DestroyImmediate(controller.gameObject);
        }
    }

    [MenuItem("Tools/Gym/Create Squat UI In Current Scene", true)]
    [MenuItem("Tools/Gym/Save New Squat UI Prefab", true)]
    private static bool CanCreate() => !Application.isPlaying;
}
