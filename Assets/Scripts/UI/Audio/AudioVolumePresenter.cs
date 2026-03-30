using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;

/// <summary>
/// 音量 Presenter 层。
/// 负责连接 View 与 Model，并在启动阶段应用已保存音量。
/// </summary>
public class AudioVolumePresenter : MonoBehaviour
{
    // 全局 Presenter 实例。
    private static AudioVolumePresenter instance;

    // 可选的 Mixer 覆盖引用；为空时从 AudioService 路由中解析。
    [Header("Mixer")]
    [SerializeField] private AudioMixer audioMixer;

    // Master 暴露参数名。
    [SerializeField] private string masterParamName = "MasterVol";

    // SFX 暴露参数名。
    [SerializeField] private string sfxParamName = "SfxVol";

    // BGM 暴露参数名。
    [SerializeField] private string bgmParamName = "BgmVol";

    // 默认 Master 线性音量。
    [Header("Default Value")]
    [SerializeField, Range(0f, 1f)] private float defaultMaster = 1f;

    // 默认 SFX 线性音量。
    [SerializeField, Range(0f, 1f)] private float defaultSfx = 1f;

    // 默认 BGM 线性音量。
    [SerializeField, Range(0f, 1f)] private float defaultBgm = 1f;

    // 线性音量接近 0 时使用的最小 dB。
    [Header("Runtime")]
    [SerializeField] private float minDb = -80f;

    // 可选默认 View 引用。
    [Header("View")]
    [SerializeField] private AudioVolumeSetting defaultView;

    // 当前 Model 实例。
    private AudioVolumeModel model;

    // 当前绑定中的 View。
    private AudioVolumeSetting boundView;

    // 单例访问器。
    public static AudioVolumePresenter Instance => instance;

    /// <summary>
    /// 初始化 Presenter。
    /// </summary>
    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(this);
            return;
        }

        instance = this;
        audioMixer = ResolveMixerReference(audioMixer);
        model = new AudioVolumeModel(
            audioMixer,
            masterParamName,
            sfxParamName,
            bgmParamName,
            defaultMaster,
            defaultSfx,
            defaultBgm,
            minDb);
    }

    /// <summary>
    /// 订阅场景回调，并尝试绑定默认 View。
    /// </summary>
    private void OnEnable()
    {
        SceneManager.sceneLoaded += HandleSceneLoaded;
        TryBindDefaultView();
    }

    /// <summary>
    /// 在首场景启动完成后应用存档音量。
    /// </summary>
    private void Start()
    {
        model?.ApplySavedToMixer();
        TryBindDefaultView();
    }

    /// <summary>
    /// 取消订阅回调，并清理绑定关系。
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        UnbindCurrentView();

        if (instance == this)
        {
            instance = null;
        }
    }

    /// <summary>
    /// 将指定 View 绑定到当前 Presenter。
    /// </summary>
    /// <param name="view">目标 View。</param>
    public void BindView(AudioVolumeSetting view)
    {
        if (view == null || model == null)
        {
            return;
        }

        AudioVolumeSnapshot snapshot = model.LoadSnapshot();

        if (boundView == view)
        {
            boundView.SetValuesWithoutNotify(snapshot);
            model.ApplySnapshotToMixer(snapshot);
            return;
        }

        UnbindCurrentView();
        boundView = view;
        boundView.MasterValueChanged += SetMasterVolume;
        boundView.SfxValueChanged += SetSfxVolume;
        boundView.BgmValueChanged += SetBgmVolume;
        boundView.SetValuesWithoutNotify(snapshot);
        model.ApplySnapshotToMixer(snapshot);
    }

    /// <summary>
    /// 若当前正绑定该 View，则解除绑定。
    /// </summary>
    /// <param name="view">目标 View。</param>
    public void UnbindView(AudioVolumeSetting view)
    {
        if (view == null || boundView != view)
        {
            return;
        }

        UnbindCurrentView();
    }

    /// <summary>
    /// 通过 Model 设置 Master 音量。
    /// </summary>
    /// <param name="value">线性音量（0~1）。</param>
    public void SetMasterVolume(float value)
    {
        model?.SetMaster(value);
    }

    /// <summary>
    /// 通过 Model 设置 SFX 音量。
    /// </summary>
    /// <param name="value">线性音量（0~1）。</param>
    public void SetSfxVolume(float value)
    {
        model?.SetSfx(value);
    }

    /// <summary>
    /// 通过 Model 设置 BGM 音量。
    /// </summary>
    /// <param name="value">线性音量（0~1）。</param>
    public void SetBgmVolume(float value)
    {
        model?.SetBgm(value);
    }

    /// <summary>
    /// 场景加载后，下一帧重应用音量并重连默认 View。
    /// </summary>
    /// <param name="scene">已加载场景。</param>
    /// <param name="mode">加载模式。</param>
    private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        StartCoroutine(ApplySavedNextFrame());
    }

    /// <summary>
    /// 等待一帧，确保目标场景中的音频对象初始化完成后再应用音量。
    /// </summary>
    private IEnumerator ApplySavedNextFrame()
    {
        yield return null;
        model?.ApplySavedToMixer();
        TryBindDefaultView();
    }

    /// <summary>
    /// 尝试绑定默认 View；若为空则自动查找（包含未激活对象）。
    /// </summary>
    private void TryBindDefaultView()
    {
        if (defaultView == null)
        {
            AudioVolumeSetting[] allViews = FindObjectsByType<AudioVolumeSetting>(
                FindObjectsInactive.Include,
                FindObjectsSortMode.None);

            Scene activeScene = SceneManager.GetActiveScene();
            for (int i = 0; i < allViews.Length; i++)
            {
                AudioVolumeSetting candidate = allViews[i];
                if (candidate != null && candidate.gameObject.scene == activeScene)
                {
                    defaultView = candidate;
                    break;
                }
            }

            if (defaultView == null && allViews.Length > 0)
            {
                defaultView = allViews[0];
            }
        }

        BindView(defaultView);
    }

    /// <summary>
    /// 解除当前绑定的 View。
    /// </summary>
    private void UnbindCurrentView()
    {
        if (boundView == null)
        {
            return;
        }

        boundView.MasterValueChanged -= SetMasterVolume;
        boundView.SfxValueChanged -= SetSfxVolume;
        boundView.BgmValueChanged -= SetBgmVolume;
        boundView = null;
    }

    /// <summary>
    /// 解析最终使用的 Mixer 引用。
    /// </summary>
    /// <param name="fallback">可选 Mixer 覆盖引用。</param>
    /// <returns>解析后的 Mixer；未找到则返回 null。</returns>
    private AudioMixer ResolveMixerReference(AudioMixer fallback)
    {
        if (fallback != null)
        {
            return fallback;
        }

        AudioService service = GetComponent<AudioService>();
        if (service == null)
        {
            service = FindFirstObjectByType<AudioService>();
        }

        if (service == null)
        {
            Debug.LogWarning("[AudioVolumePresenter] 未找到 AudioService，启动时无法应用音量。");
            return null;
        }

        // 优先使用 SFX 路由对应的 Mixer，失败再回退到 BGM 路由。
        if (service.SfxMixerGroup != null && service.SfxMixerGroup.audioMixer != null)
        {
            return service.SfxMixerGroup.audioMixer;
        }

        if (service.BgmMixerGroup != null && service.BgmMixerGroup.audioMixer != null)
        {
            return service.BgmMixerGroup.audioMixer;
        }

        Debug.LogWarning("[AudioVolumePresenter] AudioService 上未找到可用的 MixerGroup。");
        return null;
    }
}
