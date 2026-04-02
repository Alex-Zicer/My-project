using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景预热配置：按场景名组织一组预热任务。
/// </summary>
[CreateAssetMenu(fileName = "SceneWarmupProfile", menuName = "SceneLoad/Scene Warmup Profile")]
public class SceneWarmupProfileSO : ScriptableObject
{
    [Serializable]
    private class SceneWarmupEntry
    {
        // 场景名（与 Build Settings 中场景名一致）。
        public string sceneName;

        // 当前场景要执行的预热任务列表。
        public List<SceneWarmupTaskSO> tasks = new List<SceneWarmupTaskSO>();
    }

    // 所有场景共享的默认任务。
    [Header("Default Tasks")]
    [SerializeField] private List<SceneWarmupTaskSO> defaultTasks = new List<SceneWarmupTaskSO>();

    // 按场景定制的任务列表。
    [Header("Per Scene Tasks")]
    [SerializeField] private List<SceneWarmupEntry> sceneEntries = new List<SceneWarmupEntry>();

    /// <summary>
    /// 获取指定场景的最终任务列表（默认任务 + 场景专属任务）。
    /// </summary>
    /// <param name="sceneName">场景名。</param>
    /// <param name="output">输出列表（调用前无需清空）。</param>
    public void GetTasksForScene(string sceneName, List<SceneWarmupTaskSO> output)
    {
        if (output == null) return;
        output.Clear();

        AppendUniqueTasks(defaultTasks, output);

        if (string.IsNullOrWhiteSpace(sceneName) || sceneEntries == null) return;

        for (int i = 0; i < sceneEntries.Count; i++)
        {
            SceneWarmupEntry entry = sceneEntries[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.sceneName)) continue;

            if (!string.Equals(entry.sceneName, sceneName, StringComparison.OrdinalIgnoreCase)) continue;

            AppendUniqueTasks(entry.tasks, output);
        }
    }

    /// <summary>
    /// 将任务列表追加到输出并去重。
    /// </summary>
    /// <param name="source">源任务列表。</param>
    /// <param name="output">输出任务列表。</param>
    private static void AppendUniqueTasks(List<SceneWarmupTaskSO> source, List<SceneWarmupTaskSO> output)
    {
        if (source == null) return;

        for (int i = 0; i < source.Count; i++)
        {
            SceneWarmupTaskSO task = source[i];
            if (task == null || output.Contains(task)) continue;
            output.Add(task);
        }
    }
}

