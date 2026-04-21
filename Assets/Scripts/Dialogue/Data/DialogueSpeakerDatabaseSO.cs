using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话说话人资料项。
/// 用于通过 speakerId 统一管理显示名和头像。
/// </summary>
[Serializable]
public class DialogueSpeakerEntry
{
    [SerializeField] private string _speakerId;
    [SerializeField] private string _displayName;
    [SerializeField] private Sprite _portrait;

    /// <summary>
    /// 说话人唯一标识。
    /// </summary>
    public string SpeakerId => _speakerId;

    /// <summary>
    /// UI 中显示的说话人名称。
    /// </summary>
    public string DisplayName => _displayName;

    /// <summary>
    /// UI 中显示的说话人头像。
    /// </summary>
    public Sprite Portrait => _portrait;
}

/// <summary>
/// 对话说话人资料表。
/// 当前 Demo 仅用于通过 speakerId 查找显示名和头像。
/// </summary>
[CreateAssetMenu(fileName = "DialogueSpeakerDatabase", menuName = "Data/Dialogue/SpeakerDatabase")]
public class DialogueSpeakerDatabaseSO : ScriptableObject
{
    [SerializeField] private List<DialogueSpeakerEntry> _speakers = new List<DialogueSpeakerEntry>();

    // 运行时缓存，避免每次查询都遍历列表。
    private Dictionary<string, DialogueSpeakerEntry> _speakerMap;

    /// <summary>
    /// 尝试根据 speakerId 查找说话人资料。
    /// </summary>
    /// <param name="speakerId">说话人标识。</param>
    /// <param name="entry">查找到的资料项。</param>
    /// <returns>查找到返回 true，否则返回 false。</returns>
    public bool TryGetSpeaker(string speakerId, out DialogueSpeakerEntry entry)
    {
        entry = null;
        if (string.IsNullOrWhiteSpace(speakerId))
        {
            return false;
        }

        EnsureCache();
        return _speakerMap != null && _speakerMap.TryGetValue(speakerId, out entry);
    }

    /// <summary>
    /// 组件启用时重建运行时缓存。
    /// </summary>
    private void OnEnable()
    {
        RebuildCache();
    }

    /// <summary>
    /// 在编辑器修改资料表时同步刷新缓存。
    /// </summary>
    private void OnValidate()
    {
        RebuildCache();
    }

    /// <summary>
    /// 确保缓存可用。
    /// </summary>
    private void EnsureCache()
    {
        if (_speakerMap == null)
        {
            RebuildCache();
        }
    }

    /// <summary>
    /// 根据当前列表重建 speakerId 到资料项的映射。
    /// </summary>
    private void RebuildCache()
    {
        _speakerMap = new Dictionary<string, DialogueSpeakerEntry>();
        for (int i = 0; i < _speakers.Count; i++)
        {
            DialogueSpeakerEntry entry = _speakers[i];
            if (entry == null || string.IsNullOrWhiteSpace(entry.SpeakerId))
            {
                continue;
            }

            // 重复 ID 时以后出现的配置覆盖前者，保证结果稳定。
            _speakerMap[entry.SpeakerId] = entry;
        }
    }
}