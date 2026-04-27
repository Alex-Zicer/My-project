using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// 音频事件配置：定义事件标识、分类与单个 Addressables 音频引用。
/// </summary>
[CreateAssetMenu(fileName = "AudioEvent", menuName = "Audio/Audio Event")]
public class AudioEventSO : ScriptableObject
{
    // 事件唯一 ID，用于目录查询。
    [SerializeField] private string eventId;

    // 事件分类。
    [SerializeField] private AudioEventCategory category = AudioEventCategory.Sfx;

    // 该事件唯一对应的 Addressables 音频片段。
    [SerializeField] private AssetReference addressableClip;

    // 只读事件 ID。
    public string EventId => eventId;

    // 只读分类。
    public AudioEventCategory Category => category;

    // 只读 Addressables 音频引用。
    public AssetReference AddressableClip => addressableClip;
}
