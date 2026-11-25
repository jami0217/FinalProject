using System.IO;
using FinalProject.Gym.NPC;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class NPCOverthinkingMenu
{
    [MenuItem("Tools/Gym/Create NPC Overthinking System")]
    private static void CreateSystem()
    {
        OverthinkingManager existing = Object.FindFirstObjectByType<OverthinkingManager>(FindObjectsInactive.Include);
        if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
        GameObject root = NPCOverthinkingFactory.CreateSystemRoot();
        Undo.RegisterCreatedObjectUndo(root, "Create NPC Overthinking System");
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
    }

    [MenuItem("Tools/Gym/Save New Wandering NPC Prefab")]
    private static void SavePrefab()
    {
        string path = EditorUtility.SaveFilePanelInProject("Save new wandering NPC prefab", "GymNPC", "prefab", "Choose a new asset path.");
        if (string.IsNullOrEmpty(path)) return;
        if (File.Exists(path)) { Debug.LogWarning("NPC prefab was not saved: the selected file already exists."); return; }
        NPCWander2D npc = NPCOverthinkingFactory.CreateNPC(null, Vector2.zero, 0);
        try
        {
            PrefabUtility.SaveAsPrefabAsset(npc.gameObject, path);
            Selection.activeObject = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        }
        finally { Object.DestroyImmediate(npc.gameObject); }
    }

    [MenuItem("Tools/Gym/Create NPC Overthinking System", true)]
    [MenuItem("Tools/Gym/Save New Wandering NPC Prefab", true)]
    private static bool CanCreate() => !Application.isPlaying && PrefabStageUtility.GetCurrentPrefabStage() == null;
}
