using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 音频事件配置：定义事件标识、变体与运行时参数。
/// </summary>
[CreateAssetMenu(fileName = "AudioEvent", menuName = "Audio/Audio Event")]
public class AudioEventSO : ScriptableObject
{
    // 事件唯一 ID，用于目录查询。
    [SerializeField] private string eventId;

    // 事件分类。
    [SerializeField] private AudioEventCategory category = AudioEventCategory.Sfx;

    // 音频片段变体池。
    [SerializeField] private List<AudioClip> clipVariants = new List<AudioClip>();

    // 是否循环播放。
    [SerializeField] private bool loop;

    // 基础音量。
    [SerializeField, Range(0f, 1f)] private float volume = 1f;

    // 音量随机系数范围。
    [SerializeField] private Vector2 randomVolumeRange = Vector2.one;

    // 音高随机范围。
    [SerializeField] private Vector2 randomPitchRange = Vector2.one;

    // 冷却时间（秒）。
    [SerializeField, Min(0f)] private float cooldownSeconds;

    // 只读事件 ID。
    public string EventId => eventId;

    // 只读分类。
    public AudioEventCategory Category => category;

    // 只读循环标记。
    public bool Loop => loop;

    // 只读冷却秒数。
    public float CooldownSeconds => cooldownSeconds;

    /// <summary>
    /// 随机选择一个可播放的音频片段。
    /// </summary>
    /// <param name="clip">输出片段。</param>
    /// <returns>选择成功返回 true。</returns>
    public bool TryPickClip(out AudioClip clip)
    {
        clip = null;
        if (clipVariants == null || clipVariants.Count == 0)
        {
            return false;
        }

        int index = Random.Range(0, clipVariants.Count);
        clip = clipVariants[index];
        return clip != null;
    }

    /// <summary>
    /// 获取本次播放的运行时音量。
    /// </summary>
    /// <returns>0~1 的音量值。</returns>
    public float GetRuntimeVolume()
    {
        float min = Mathf.Min(randomVolumeRange.x, randomVolumeRange.y);
        float max = Mathf.Max(randomVolumeRange.x, randomVolumeRange.y);
        float randomScale = Random.Range(min, max);
        return Mathf.Clamp01(volume * randomScale);
    }

    /// <summary>
    /// 获取本次播放的运行时音高。
    /// </summary>
    /// <returns>音高值。</returns>
    public float GetRuntimePitch()
    {
        float min = Mathf.Min(randomPitchRange.x, randomPitchRange.y);
        float max = Mathf.Max(randomPitchRange.x, randomPitchRange.y);
        return Random.Range(min, max);
    }
}
