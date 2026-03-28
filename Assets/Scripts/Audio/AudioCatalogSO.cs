using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 音频事件目录：按 UI/SFX/BGM 分类管理，并提供 eventId 查询能力。
/// </summary>
[CreateAssetMenu(fileName = "AudioCatalog", menuName = "Audio/Audio Catalog")]
public class AudioCatalogSO : ScriptableObject
{
    // UI 分类事件列表。
    [Header("UI Events")]
    [SerializeField] private List<AudioEventSO> uiEvents = new List<AudioEventSO>();

    // SFX 分类事件列表。
    [Header("SFX Events")]
    [SerializeField] private List<AudioEventSO> sfxEvents = new List<AudioEventSO>();

    // BGM 分类事件列表。
    [Header("BGM Events")]
    [SerializeField] private List<AudioEventSO> bgmEvents = new List<AudioEventSO>();

    // 旧版单列表字段：用于兼容迁移。
    [FormerlySerializedAs("events")]
    [HideInInspector]
    [SerializeField] private List<AudioEventSO> legacyEvents = new List<AudioEventSO>();

    // 运行时查询缓存：eventId -> event。
    private Dictionary<string, AudioEventSO> cacheById;

    // 分类查询缓存。
    private readonly Dictionary<AudioEventCategory, List<AudioEventSO>> cacheByCategory =
        new Dictionary<AudioEventCategory, List<AudioEventSO>>();

    /// <summary>
    /// 资源启用时重建查询缓存。
    /// </summary>
    private void OnEnable()
    {
        RebuildCache();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器字段变更时重建缓存，保持查询结果一致。
    /// </summary>
    private void OnValidate()
    {
        RebuildCache();
    }
#endif

    /// <summary>
    /// 根据事件 ID 获取事件，未命中返回 null。
    /// </summary>
    /// <param name="eventId">事件 ID。</param>
    /// <returns>命中的事件或 null。</returns>
    public AudioEventSO GetEventOrNull(string eventId)
    {
        if (string.IsNullOrWhiteSpace(eventId))
        {
            return null;
        }

        if (cacheById == null)
        {
            RebuildCache();
        }

        cacheById.TryGetValue(eventId, out AudioEventSO evt);
        return evt;
    }

    /// <summary>
    /// 根据事件 ID 尝试获取事件。
    /// </summary>
    /// <param name="eventId">事件 ID。</param>
    /// <param name="evt">输出事件。</param>
    /// <returns>命中返回 true。</returns>
    public bool TryGetEvent(string eventId, out AudioEventSO evt)
    {
        evt = GetEventOrNull(eventId);
        return evt != null;
    }

    /// <summary>
    /// 获取指定分类下的事件列表（只读）。
    /// </summary>
    /// <param name="category">事件分类。</param>
    /// <returns>该分类事件列表。</returns>
    public IReadOnlyList<AudioEventSO> GetEventsByCategory(AudioEventCategory category)
    {
        if (cacheById == null)
        {
            RebuildCache();
        }

        if (!cacheByCategory.TryGetValue(category, out List<AudioEventSO> list))
        {
            return System.Array.Empty<AudioEventSO>();
        }

        return list;
    }

    /// <summary>
    /// 重建事件缓存。
    /// </summary>
    private void RebuildCache()
    {
        // 优先把旧版单列表按 Category 自动迁移到三分类列表。
        MigrateLegacyEventsIfNeeded();

        cacheById = new Dictionary<string, AudioEventSO>();
        cacheByCategory[AudioEventCategory.UI] = new List<AudioEventSO>();
        cacheByCategory[AudioEventCategory.Sfx] = new List<AudioEventSO>();
        cacheByCategory[AudioEventCategory.Bgm] = new List<AudioEventSO>();

        AddEventsToCache(uiEvents, AudioEventCategory.UI);
        AddEventsToCache(sfxEvents, AudioEventCategory.Sfx);
        AddEventsToCache(bgmEvents, AudioEventCategory.Bgm);
    }

    /// <summary>
    /// 将一组事件写入缓存并做分类一致性校验。
    /// </summary>
    /// <param name="source">源列表。</param>
    /// <param name="expectedCategory">预期分类。</param>
    private void AddEventsToCache(List<AudioEventSO> source, AudioEventCategory expectedCategory)
    {
        if (source == null)
        {
            return;
        }

        List<AudioEventSO> categoryList = cacheByCategory[expectedCategory];
        for (int i = 0; i < source.Count; i++)
        {
            AudioEventSO evt = source[i];
            if (evt == null || string.IsNullOrWhiteSpace(evt.EventId))
            {
                continue;
            }

            // Inspector 列表分类与事件自身分类不一致时给出提示，便于维护。
            if (evt.Category != expectedCategory)
            {
                Debug.LogWarning(
                    $"[AudioCatalogSO] 事件 '{evt.EventId}' 位于 {expectedCategory} 列表，但自身分类是 {evt.Category}。",
                    this);
            }

            // 重复 key 以后者为准，便于覆盖旧配置。
            if (cacheById.ContainsKey(evt.EventId))
            {
                Debug.LogWarning($"[AudioCatalogSO] 检测到重复 eventId：{evt.EventId}，将以后者覆盖前者。", this);
            }

            cacheById[evt.EventId] = evt;
            categoryList.Add(evt);
        }
    }

    /// <summary>
    /// 将旧版单列表自动迁移到三分类列表。
    /// </summary>
    private void MigrateLegacyEventsIfNeeded()
    {
        if (legacyEvents == null || legacyEvents.Count == 0)
        {
            return;
        }

        // 仅在新列表为空时执行自动迁移，避免覆盖手工整理结果。
        bool hasNewData =
            (uiEvents != null && uiEvents.Count > 0) ||
            (sfxEvents != null && sfxEvents.Count > 0) ||
            (bgmEvents != null && bgmEvents.Count > 0);

        if (hasNewData)
        {
            return;
        }

        for (int i = 0; i < legacyEvents.Count; i++)
        {
            AudioEventSO evt = legacyEvents[i];
            if (evt == null)
            {
                continue;
            }

            switch (evt.Category)
            {
                case AudioEventCategory.UI:
                    uiEvents.Add(evt);
                    break;
                case AudioEventCategory.Bgm:
                    bgmEvents.Add(evt);
                    break;
                default:
                    sfxEvents.Add(evt);
                    break;
            }
        }

        legacyEvents.Clear();
    }
}
