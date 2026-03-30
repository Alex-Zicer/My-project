using UnityEngine;
using UnityEngine.Audio;

/// <summary>
/// 音量 Model 层。
/// 负责 PlayerPrefs 持久化与 Mixer 写入。
/// </summary>
public sealed class AudioVolumeModel
{
    // Master 存档键。
    private const string MasterPrefsKey = "audio.master";

    // SFX 存档键。
    private const string SfxPrefsKey = "audio.sfx";

    // BGM 存档键。
    private const string BgmPrefsKey = "audio.bgm";

    // 目标 AudioMixer。
    private readonly AudioMixer audioMixer;

    // Master 暴露参数名。
    private readonly string masterParamName;

    // SFX 暴露参数名。
    private readonly string sfxParamName;

    // BGM 暴露参数名。
    private readonly string bgmParamName;

    // 默认 Master 音量。
    private readonly float defaultMaster;

    // 默认 SFX 音量。
    private readonly float defaultSfx;

    // 默认 BGM 音量。
    private readonly float defaultBgm;

    // 线性音量接近 0 时使用的最小 dB。
    private readonly float minDb;

    /// <summary>
    /// 构造 Model 实例。
    /// </summary>
    /// <param name="mixer">目标 Mixer。</param>
    /// <param name="masterParam">Master 暴露参数名。</param>
    /// <param name="sfxParam">SFX 暴露参数名。</param>
    /// <param name="bgmParam">BGM 暴露参数名。</param>
    /// <param name="masterDefault">默认 Master 音量（0~1）。</param>
    /// <param name="sfxDefault">默认 SFX 音量（0~1）。</param>
    /// <param name="bgmDefault">默认 BGM 音量（0~1）。</param>
    /// <param name="minDbValue">最小 dB 值。</param>
    public AudioVolumeModel(
        AudioMixer mixer,
        string masterParam,
        string sfxParam,
        string bgmParam,
        float masterDefault,
        float sfxDefault,
        float bgmDefault,
        float minDbValue)
    {
        audioMixer = mixer;
        masterParamName = masterParam;
        sfxParamName = sfxParam;
        bgmParamName = bgmParam;
        defaultMaster = Mathf.Clamp01(masterDefault);
        defaultSfx = Mathf.Clamp01(sfxDefault);
        defaultBgm = Mathf.Clamp01(bgmDefault);
        minDb = minDbValue;
    }

    /// <summary>
    /// 从 PlayerPrefs 读取当前音量快照。
    /// </summary>
    /// <returns>当前音量快照。</returns>
    public AudioVolumeSnapshot LoadSnapshot()
    {
        float master = LoadVolume(MasterPrefsKey, defaultMaster);
        float sfx = LoadVolume(SfxPrefsKey, defaultSfx);
        float bgm = LoadVolume(BgmPrefsKey, defaultBgm);
        return new AudioVolumeSnapshot(master, sfx, bgm);
    }

    /// <summary>
    /// 将已保存音量应用到 Mixer。
    /// </summary>
    public void ApplySavedToMixer()
    {
        ApplySnapshotToMixer(LoadSnapshot());
    }

    /// <summary>
    /// 将快照音量应用到 Mixer。
    /// </summary>
    /// <param name="snapshot">音量快照。</param>
    public void ApplySnapshotToMixer(AudioVolumeSnapshot snapshot)
    {
        ApplyMixerVolume(masterParamName, snapshot.Master);
        ApplyMixerVolume(sfxParamName, snapshot.Sfx);
        ApplyMixerVolume(bgmParamName, snapshot.Bgm);
    }

    /// <summary>
    /// 设置 Master 音量并持久化。
    /// </summary>
    /// <param name="value">线性音量（0~1）。</param>
    public void SetMaster(float value)
    {
        float normalized = Mathf.Clamp01(value);
        SaveVolume(MasterPrefsKey, normalized);
        ApplyMixerVolume(masterParamName, normalized);
    }

    /// <summary>
    /// 设置 SFX 音量并持久化。
    /// </summary>
    /// <param name="value">线性音量（0~1）。</param>
    public void SetSfx(float value)
    {
        float normalized = Mathf.Clamp01(value);
        SaveVolume(SfxPrefsKey, normalized);
        ApplyMixerVolume(sfxParamName, normalized);
    }

    /// <summary>
    /// 设置 BGM 音量并持久化。
    /// </summary>
    /// <param name="value">线性音量（0~1）。</param>
    public void SetBgm(float value)
    {
        float normalized = Mathf.Clamp01(value);
        SaveVolume(BgmPrefsKey, normalized);
        ApplyMixerVolume(bgmParamName, normalized);
    }

    /// <summary>
    /// 从 PlayerPrefs 读取线性音量。
    /// </summary>
    /// <param name="prefsKey">存档键。</param>
    /// <param name="defaultValue">默认值。</param>
    /// <returns>夹取后的 0~1 音量。</returns>
    private static float LoadVolume(string prefsKey, float defaultValue)
    {
        return Mathf.Clamp01(PlayerPrefs.GetFloat(prefsKey, defaultValue));
    }

    /// <summary>
    /// 将线性音量写入 PlayerPrefs。
    /// </summary>
    /// <param name="prefsKey">存档键。</param>
    /// <param name="value">线性音量（0~1）。</param>
    private static void SaveVolume(string prefsKey, float value)
    {
        PlayerPrefs.SetFloat(prefsKey, Mathf.Clamp01(value));
    }

    /// <summary>
    /// 将线性音量写入 Mixer 参数。
    /// </summary>
    /// <param name="paramName">暴露参数名。</param>
    /// <param name="linearValue">线性音量（0~1）。</param>
    private void ApplyMixerVolume(string paramName, float linearValue)
    {
        if (audioMixer == null || string.IsNullOrWhiteSpace(paramName))
        {
            return;
        }

        float db = LinearToDb(linearValue);
        audioMixer.SetFloat(paramName, db);
    }

    /// <summary>
    /// 将线性音量转换为 dB。
    /// </summary>
    /// <param name="linearValue">线性音量（0~1）。</param>
    /// <returns>dB 值。</returns>
    private float LinearToDb(float linearValue)
    {
        float normalized = Mathf.Clamp01(linearValue);
        if (normalized <= 0.0001f)
        {
            return minDb;
        }

        // AudioMixer 使用对数刻度，线性音量需要换算为 dB。
        return Mathf.Log10(normalized) * 20f;
    }
}
