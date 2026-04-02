using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频预热任务：按事件列表提前加载音频数据。
/// </summary>
[CreateAssetMenu(fileName = "AudioWarmupTask", menuName = "SceneLoad/Warmup Tasks/Audio Warmup Task")]
public class AudioWarmupTaskSO : SceneWarmupTaskSO
{
    // 直接引用的音效事件列表。
    [Header("Direct Audio Events")]
    [SerializeField] private List<AudioEventSO> audioEvents = new List<AudioEventSO>();

    // 通过 eventId 查找的事件列表（便于轻量配置）。
    [Header("Audio Event IDs")]
    [SerializeField] private List<string> audioEventIds = new List<string>();

    /// <summary>
    /// 执行音频预热。
    /// </summary>
    /// <param name="sceneName">当前场景名。</param>
    /// <param name="reportProgress">进度回调（0~1）。</param>
    /// <returns></returns>
    public override IEnumerator RunWarmup(string sceneName, Action<float> reportProgress)
    {
        AudioService service = AudioService.Instance;
        if (service == null)
        {
            reportProgress?.Invoke(1f);
            yield break;
        }

        List<AudioEventSO> eventsToWarmup = BuildWarmupEvents(service);
        if (eventsToWarmup.Count == 0)
        {
            reportProgress?.Invoke(1f);
            yield break;
        }

        yield return service.PrewarmAudioEvents(eventsToWarmup, reportProgress);
    }

    /// <summary>
    /// 组装并去重需要预热的音效事件列表。
    /// </summary>
    /// <param name="service">音频服务实例。</param>
    /// <returns>事件列表。</returns>
    private List<AudioEventSO> BuildWarmupEvents(AudioService service)
    {
        var result = new List<AudioEventSO>();
        var seen = new HashSet<AudioEventSO>();

        for (int i = 0; i < audioEvents.Count; i++)
        {
            AudioEventSO evt = audioEvents[i];
            if (evt == null || !seen.Add(evt)) continue;
            result.Add(evt);
        }

        for (int i = 0; i < audioEventIds.Count; i++)
        {
            string eventId = audioEventIds[i];
            if (string.IsNullOrWhiteSpace(eventId)) continue;

            AudioEventSO evt = service.GetEventOrNull(eventId);
            if (evt == null || !seen.Add(evt)) continue;
            result.Add(evt);
        }

        return result;
    }
}

