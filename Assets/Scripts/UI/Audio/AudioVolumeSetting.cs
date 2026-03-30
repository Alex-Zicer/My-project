using System;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 音量 View 层。
/// 仅负责滑块显示与输入转发。
/// </summary>
public class AudioVolumeSetting : MonoBehaviour
{
    // Master 音量滑块。
    [Header("Sliders")]
    [SerializeField] private Slider masterSlider;

    // SFX 音量滑块。
    [SerializeField] private Slider sfxSlider;

    // BGM 音量滑块。
    [SerializeField] private Slider bgmSlider;

    // 可选 Presenter 引用；为空时自动解析。
    [Header("Presenter")]
    [SerializeField] private AudioVolumePresenter presenter;

    // 标记滑块回调是否已注册。
    private bool hasRegisteredCallbacks;

    // Master 音量变更事件。
    public event Action<float> MasterValueChanged;

    // SFX 音量变更事件。
    public event Action<float> SfxValueChanged;

    // BGM 音量变更事件。
    public event Action<float> BgmValueChanged;

    /// <summary>
    /// 启用时绑定 Presenter 并注册滑块回调。
    /// </summary>
    private void OnEnable()
    {
        EnsurePresenter();
        presenter?.BindView(this);
        RegisterSliderCallbacks();
    }

    /// <summary>
    /// 禁用时反注册回调并解除 Presenter 绑定。
    /// </summary>
    private void OnDisable()
    {
        UnregisterSliderCallbacks();
        presenter?.UnbindView(this);
    }

    /// <summary>
    /// 不触发回调地刷新滑块显示值。
    /// </summary>
    /// <param name="snapshot">音量快照。</param>
    public void SetValuesWithoutNotify(AudioVolumeSnapshot snapshot)
    {
        if (masterSlider != null)
        {
            masterSlider.SetValueWithoutNotify(snapshot.Master);
        }

        if (sfxSlider != null)
        {
            sfxSlider.SetValueWithoutNotify(snapshot.Sfx);
        }

        if (bgmSlider != null)
        {
            bgmSlider.SetValueWithoutNotify(snapshot.Bgm);
        }
    }

    /// <summary>
    /// 注册一次滑块回调，避免重复注册。
    /// </summary>
    private void RegisterSliderCallbacks()
    {
        if (hasRegisteredCallbacks)
        {
            return;
        }

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.AddListener(HandleMasterSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.AddListener(HandleSfxSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.AddListener(HandleBgmSliderChanged);
        }

        hasRegisteredCallbacks = true;
    }

    /// <summary>
    /// 反注册滑块回调。
    /// </summary>
    private void UnregisterSliderCallbacks()
    {
        if (!hasRegisteredCallbacks)
        {
            return;
        }

        if (masterSlider != null)
        {
            masterSlider.onValueChanged.RemoveListener(HandleMasterSliderChanged);
        }

        if (sfxSlider != null)
        {
            sfxSlider.onValueChanged.RemoveListener(HandleSfxSliderChanged);
        }

        if (bgmSlider != null)
        {
            bgmSlider.onValueChanged.RemoveListener(HandleBgmSliderChanged);
        }

        hasRegisteredCallbacks = false;
    }

    /// <summary>
    /// 确保 Presenter 引用可用。
    /// </summary>
    /// <returns>存在 Presenter 时返回 true。</returns>
    private bool EnsurePresenter()
    {
        if (presenter != null)
        {
            return true;
        }

        presenter = AudioVolumePresenter.Instance;
        if (presenter != null)
        {
            return true;
        }

        presenter = FindFirstObjectByType<AudioVolumePresenter>();
        return presenter != null;
    }

    /// <summary>
    /// 处理 Master 滑块值变更。
    /// </summary>
    /// <param name="value">线性音量（0~1）。</param>
    private void HandleMasterSliderChanged(float value)
    {
        MasterValueChanged?.Invoke(value);
    }

    /// <summary>
    /// 处理 SFX 滑块值变更。
    /// </summary>
    /// <param name="value">线性音量（0~1）。</param>
    private void HandleSfxSliderChanged(float value)
    {
        SfxValueChanged?.Invoke(value);
    }

    /// <summary>
    /// 处理 BGM 滑块值变更。
    /// </summary>
    /// <param name="value">线性音量（0~1）。</param>
    private void HandleBgmSliderChanged(float value)
    {
        BgmValueChanged?.Invoke(value);
    }
}
