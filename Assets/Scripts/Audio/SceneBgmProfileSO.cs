using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 场景 BGM 映射配置：当前仅提供数据结构与查询能力。
/// </summary>
[CreateAssetMenu(fileName = "SceneBgmProfile", menuName = "Audio/Scene BGM Profile")]
public class SceneBgmProfileSO : ScriptableObject
{
    /// <summary>
    /// 场景与 BGM 事件映射项。
    /// </summary>
    [Serializable]
    public struct SceneBgmEntry
    {
        // 场景名。
        public string sceneName;

        // 对应 BGM 事件。
        public AudioEventSO bgmEvent;
    }

    // 场景映射列表。
    [SerializeField] private List<SceneBgmEntry> entries = new List<SceneBgmEntry>();

    /// <summary>
    /// 根据场景名查询 BGM 事件。
    /// </summary>
    /// <param name="sceneName">场景名。</param>
    /// <param name="evt">输出 BGM 事件。</param>
    /// <returns>查找成功返回 true。</returns>
    public bool TryGetBgmEvent(string sceneName, out AudioEventSO evt)
    {
        evt = null;
        if (string.IsNullOrWhiteSpace(sceneName))
        {
            return false;
        }

        for (int i = 0; i < entries.Count; i++)
        {
            if (!string.Equals(entries[i].sceneName, sceneName, StringComparison.Ordinal))
            {
                continue;
            }

            evt = entries[i].bgmEvent;
            return evt != null;
        }

        return false;
    }
}
