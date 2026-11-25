using FinalProject.Gym.Core;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

public static class GymCoreMenu
{
    [MenuItem("Tools/Gym/Create Anxiety Core System")]
    private static void Create()
    {
        GymGameBridge existing = Object.FindFirstObjectByType<GymGameBridge>(FindObjectsInactive.Include);
        if (existing != null) { Selection.activeGameObject = existing.gameObject; return; }
        GameObject root = GymCoreFactory.CreateSystem();
        Undo.RegisterCreatedObjectUndo(root, "Create Gym Anxiety Core");
        EditorSceneManager.MarkSceneDirty(root.scene);
        Selection.activeGameObject = root;
    }
    [MenuItem("Tools/Gym/Create Anxiety Core System", true)]
    private static bool CanCreate() => !Application.isPlaying && PrefabStageUtility.GetCurrentPrefabStage() == null;
}

[CustomEditor(typeof(GymProgressManager))]
public sealed class GymProgressManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();
        GymProgressManager progress = (GymProgressManager)target;
        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("以下測試按鈕只存在 Unity Editor，正式遊戲不會提供模擬完成入口。", MessageType.Info);
        using (new EditorGUI.DisabledScope(!Application.isPlaying || (GymGameManager.Instance != null && GymGameManager.Instance.IsMiniGameOpen)))
        {
            if (!Application.isPlaying) return;
            foreach (GymStationDefinition station in progress.Stations)
                if (GUILayout.Button("模擬完成：" + station.displayName)) { progress.StartStation(station.id); progress.CompleteStation(station.id); }
        }
    }
}
