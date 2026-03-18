using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class MissingScriptScanner
{
    [MenuItem("Tools/Diagnostics/Scan Missing Scripts (Open Scenes)")]
    public static void ScanOpenScenes()
    {
        int totalMissing = 0;

        for (int i = 0; i < SceneManager.sceneCount; i++)
        {
            Scene scene = SceneManager.GetSceneAt(i);
            if (!scene.isLoaded) continue;

            foreach (var root in scene.GetRootGameObjects())
            {
                totalMissing += ScanGameObjectTree(root, $"{scene.name}/{root.name}");
            }
        }

        Debug.Log($"[MissingScriptScanner] Open scenes scan complete. Missing scripts found: {totalMissing}");
    }

    [MenuItem("Tools/Diagnostics/Scan Missing Scripts (All Prefabs)")]
    public static void ScanAllPrefabs()
    {
        string[] prefabGuids = AssetDatabase.FindAssets("t:Prefab");
        int totalMissing = 0;

        try
        {
            for (int i = 0; i < prefabGuids.Length; i++)
            {
                string path = AssetDatabase.GUIDToAssetPath(prefabGuids[i]);
                var prefabRoot = PrefabUtility.LoadPrefabContents(path);
                if (prefabRoot == null) continue;

                totalMissing += ScanGameObjectTree(prefabRoot, path);

                PrefabUtility.UnloadPrefabContents(prefabRoot);
            }
        }
        finally
        {
            Debug.Log($"[MissingScriptScanner] Prefab scan complete. Missing scripts found: {totalMissing}");
        }
    }

    [MenuItem("Tools/Diagnostics/Scan Missing Scripts (Build Settings Scenes)")]
    public static void ScanBuildScenes()
    {
        var scenes = EditorBuildSettings.scenes;
        int totalMissing = 0;

        string activeScenePath = SceneManager.GetActiveScene().path;

        try
        {
            foreach (var s in scenes)
            {
                if (!s.enabled) continue;
                var scene = EditorSceneManager.OpenScene(s.path, OpenSceneMode.Single);

                foreach (var root in scene.GetRootGameObjects())
                {
                    totalMissing += ScanGameObjectTree(root, $"{scene.name}/{root.name}");
                }
            }
        }
        finally
        {
            if (!string.IsNullOrWhiteSpace(activeScenePath))
            {
                EditorSceneManager.OpenScene(activeScenePath, OpenSceneMode.Single);
            }

            Debug.Log($"[MissingScriptScanner] Build scenes scan complete. Missing scripts found: {totalMissing}");
        }
    }

    private static int ScanGameObjectTree(GameObject go, string path)
    {
        int missing = 0;

        int missingCount = GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(go);
        if (missingCount > 0)
        {
            missing += missingCount;
            Debug.LogWarning($"[MissingScriptScanner] Missing script x{missingCount} on: {path}", go);
        }

        Transform t = go.transform;
        for (int i = 0; i < t.childCount; i++)
        {
            var child = t.GetChild(i);
            missing += ScanGameObjectTree(child.gameObject, $"{path}/{child.name}");
        }

        return missing;
    }
}

