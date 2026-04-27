using System;
using UnityEngine;

/// <summary>
/// 通用音效发射器（Catalog-only）：
/// 动画事件传入 eventId，统一通过 AudioCatalog 查找并播放。
/// </summary>
public class ActionSfxEmitter : MonoBehaviour, IActionSfxEmitter
{
    private const string LoopAudioSourceName = "LoopSfxSource";

    // 是否静音当前发射器。
    [Header("Flags")]
    [SerializeField] private bool muteThisEmitter;

    // 未找到 eventId 时是否打印警告。
    [SerializeField] private bool logWarningWhenMissingKey = true;

    // 当前循环音效源。
    private AudioSource loopSource;

    // 当前循环音效事件 ID。
    private string currentLoopEventId;

    /// <summary>
    /// 组件禁用时停止循环音效，避免残留播放。
    /// </summary>
    private void OnDisable()
    {
        StopLoopSfx(string.Empty);
    }

    /// <summary>
    /// 按事件 ID 播放一次性音效。
    /// </summary>
    /// <param name="eventId">音效事件 ID。</param>
    public void PlaySfx(string eventId)
    {
        TryPlaySfx(eventId);
    }

    /// <summary>
    /// 尝试按事件 ID 播放一次性音效。
    /// </summary>
    /// <param name="eventId">音效事件 ID。</param>
    /// <returns>播放成功返回 true。</returns>
    public bool TryPlaySfx(string eventId)
    {
        if (!TryResolveSfxEvent(eventId, out AudioEventSO evt))
        {
            return false;
        }

        AudioService.Instance.PlaySfx2D(evt);
        return true;
    }

    /// <summary>
    /// 开始播放循环音效。
    /// </summary>
    /// <param name="eventId">音效事件 ID。</param>
    public void PlayLoopSfx(string eventId)
    {
        TryPlayLoopSfx(eventId);
    }

    /// <summary>
    /// 尝试开始播放循环音效。
    /// </summary>
    /// <param name="eventId">音效事件 ID。</param>
    /// <returns>开始请求成功返回 true。</returns>
    public bool TryPlayLoopSfx(string eventId)
    {
        if (!TryResolveSfxEvent(eventId, out AudioEventSO evt))
        {
            return false;
        }

        if (loopSource == null)
        {
            loopSource = CreateLoopSource();
        }

        // 先记录目标循环事件，异步加载回调会据此确认当前请求是否仍然有效。
        currentLoopEventId = evt.EventId;

        AudioService.Instance.RequestAudioClip(evt, clip =>
        {
            if (clip == null || loopSource == null)
            {
                return;
            }

            // 如果异步返回时当前循环请求已经被取消或切换，则忽略过期结果。
            if (!string.Equals(currentLoopEventId, evt.EventId, StringComparison.Ordinal))
            {
                return;
            }

            loopSource.Stop();
            loopSource.clip = clip;
            loopSource.loop = true;
            loopSource.pitch = 1f;
            loopSource.volume = 1f;
            loopSource.outputAudioMixerGroup = AudioService.Instance.SfxMixerGroup;
            loopSource.Play();
        });

        return true;
    }

    /// <summary>
    /// 停止当前循环音效；若传入 eventId，则只在匹配当前循环时停止。
    /// </summary>
    /// <param name="eventId">要停止的循环音效 ID。</param>
    public void StopLoopSfx(string eventId)
    {
        if (!string.IsNullOrWhiteSpace(eventId) &&
            !string.Equals(currentLoopEventId, eventId, StringComparison.Ordinal))
        {
            return;
        }

        // 先清掉当前循环 ID，避免异步加载完成后又把已取消的循环重新播出来。
        currentLoopEventId = null;

        if (loopSource == null)
        {
            return;
        }

        loopSource.Stop();
        loopSource.clip = null;
    }

    /// <summary>
    /// 验证并解析 SFX 事件。
    /// </summary>
    private bool TryResolveSfxEvent(string eventId, out AudioEventSO evt)
    {
        evt = null;

        if (muteThisEmitter || string.IsNullOrWhiteSpace(eventId))
        {
            return false;
        }

        evt = AudioService.Instance.GetEventOrNull(eventId);
        if (evt == null)
        {
            if (logWarningWhenMissingKey)
            {
                Debug.LogWarning($"[ActionSfxEmitter] 未找到动作音效事件：{eventId}", this);
            }

            return false;
        }

        if (evt.Category != AudioEventCategory.Sfx)
        {
            Debug.LogWarning(
                $"[ActionSfxEmitter] 事件 '{evt.EventId}' 分类为 {evt.Category}，动作音效应使用 Sfx 分类。",
                this);
            evt = null;
            return false;
        }

        return true;
    }

    /// <summary>
    /// 为当前发射器创建一个专用循环音效源。
    /// </summary>
    private AudioSource CreateLoopSource()
    {
        Transform existingChild = transform.Find(LoopAudioSourceName);
        GameObject loopSourceObject;
        if (existingChild != null)
        {
            loopSourceObject = existingChild.gameObject;
        }
        else
        {
            loopSourceObject = new GameObject(LoopAudioSourceName);
            loopSourceObject.transform.SetParent(transform, false);
        }

        AudioSource source = loopSourceObject.GetComponent<AudioSource>();
        if (source == null)
        {
            source = loopSourceObject.AddComponent<AudioSource>();
        }

        // 循环音效走独立 AudioSource，避免和一次性动作音效互相打断。
        source.playOnAwake = false;
        source.loop = true;
        source.spatialBlend = 0f;
        source.outputAudioMixerGroup = AudioService.Instance.SfxMixerGroup;
        return source;
    }
}
