using System;
using System.Collections;
using UnityEngine;

/// <summary>
/// 场景预热任务抽象。
/// 由 SceneLoader 在黑屏阶段统一调度执行。
/// </summary>
public abstract class SceneWarmupTaskSO : ScriptableObject
{
    // 任务显示名（可选）。为空时回退资产名。
    [SerializeField] private string taskName;

    // 任务名只读暴露。
    public string TaskName => string.IsNullOrWhiteSpace(taskName) ? name : taskName;

    /// <summary>
    /// 执行预热任务。
    /// </summary>
    /// <param name="sceneName">当前场景名。</param>
    /// <param name="reportProgress">进度回调（0~1）。</param>
    /// <returns></returns>
    public abstract IEnumerator RunWarmup(string sceneName, Action<float> reportProgress);
}

