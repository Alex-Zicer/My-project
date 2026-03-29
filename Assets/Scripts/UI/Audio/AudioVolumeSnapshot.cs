using UnityEngine;

/// <summary>
/// 不可变的音量快照。
/// </summary>
public readonly struct AudioVolumeSnapshot
{
    // Master 线性音量。
    public readonly float Master;

    // SFX 线性音量。
    public readonly float Sfx;

    // BGM 线性音量。
    public readonly float Bgm;

    /// <summary>
    /// 构造并夹取音量快照。
    /// </summary>
    /// <param name="master">Master 线性音量（0~1）。</param>
    /// <param name="sfx">SFX 线性音量（0~1）。</param>
    /// <param name="bgm">BGM 线性音量（0~1）。</param>
    public AudioVolumeSnapshot(float master, float sfx, float bgm)
    {
        Master = Mathf.Clamp01(master);
        Sfx = Mathf.Clamp01(sfx);
        Bgm = Mathf.Clamp01(bgm);
    }
}
