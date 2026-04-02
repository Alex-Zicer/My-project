using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    // 全局单例，供调试器或其他系统直接调用相机反馈。
    public static CameraManager Instance { get; private set; }

    // 玩家主虚拟相机引用，用于获取 Noise 模块与镜头参数。
    [SerializeField] private CinemachineVirtualCamera playerCamera;
    // Noise 模块引用，负责控制抖动强度与频率。
    private CinemachineBasicMultiChannelPerlin _perlin;

    // 当前场景中的玩家引用（命中事件来源）。
    private PlayerController _player;
    // 分别持有协程引用，防止 Shake 和 HitStop 互相打断。
    private Coroutine _shakeCoroutine;
    private Coroutine _hitStopCoroutine;
    // 当前是否处于帧冻结阶段。
    private bool _isHitStopping;
    // 进入帧冻结前的时间缩放值，用于冻结结束后恢复。
    private float _timeScaleBeforeHitStop = 1f;
    // 默认镜头 Dutch 值（防止抖动后残留倾斜）。
    private float _defaultDutch;
    // 默认虚拟相机本地旋转（防止抖动后残留倾斜）。
    private Quaternion _defaultVirtualCameraLocalRotation;
    // 默认 Noise 节点本地旋转（防止噪声旋转残留）。
    private Quaternion _defaultNoiseNodeLocalRotation;

    /// <summary>
    /// 初始化单例、相机模块与默认镜头姿态。
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

        if (playerCamera == null)
        {
            Debug.LogError("CameraManager 未绑定 playerCamera。", this);
            return;
        }

        // 获取 Noise 模块的引用
        _perlin = playerCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        if (_perlin == null)
        {
            Debug.LogError("playerCamera 未找到 CinemachineBasicMultiChannelPerlin 组件。", playerCamera);
            return;
        }

        _defaultDutch = playerCamera.m_Lens.Dutch;
        _defaultVirtualCameraLocalRotation = playerCamera.transform.localRotation;
        _defaultNoiseNodeLocalRotation = _perlin.transform.localRotation;
        // 将参数初始化为 0，避免开局镜头抖动
        _perlin.m_AmplitudeGain = 0;
        _perlin.m_FrequencyGain = 0;
        ResetCameraTilt();
    }

    /// <summary>
    /// 启动时绑定当前场景中的 PlayerController。
    /// </summary>
    private void Start()
    {
        // Start 里找 Player，确保 PlayerController.Awake 已执行完毕
        BindPlayer(FindObjectOfType<PlayerController>());
    }

    /// <summary>
    /// 订阅场景加载事件，支持切场景后自动重新绑定玩家。
    /// </summary>
    private void OnEnable()
    {
        // 场景切换后重新绑定新场景的 PlayerController（CameraManager 是 DontDestroyOnLoad）
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    /// <summary>
    /// 取消订阅场景事件，并在必要时恢复 timeScale。
    /// </summary>
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        RestoreTimeScaleIfNeeded();
    }

    /// <summary>
    /// 场景加载完成后重新绑定玩家事件。
    /// </summary>
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnbindPlayer();
        BindPlayer(FindObjectOfType<PlayerController>());
    }

    /// <summary>
    /// 绑定玩家并订阅“攻击命中”事件。
    /// </summary>
    /// <param name="player">当前场景玩家。</param>
    private void BindPlayer(PlayerController player)
    {
        _player = player;
        if (_player == null) return;
        _player.OnAttackHit += OnAttackHit;
    }

    /// <summary>
    /// 解绑玩家并取消事件订阅。
    /// </summary>
    private void UnbindPlayer()
    {
        if (_player == null) return;
        _player.OnAttackHit -= OnAttackHit;
        _player = null;
    }

    /// <summary>
    /// 对象销毁时清理绑定并恢复 timeScale。
    /// </summary>
    private void OnDestroy()
    {
        UnbindPlayer();
        RestoreTimeScaleIfNeeded();
    }

    /// <summary>
    /// 处理“攻击命中敌人”事件：同时触发帧冻结与镜头抖动。
    /// </summary>
    private void OnAttackHit()
    {
        if (_player == null || _player.PlayerData == null) return;

        // 先冻结，再抖动，增强命中瞬间的冲击感。
        HitStop(_player.PlayerData.attackHitStopDuration);
        Shake(
            _player.PlayerData.attackShakeIntensity,
            _player.PlayerData.attackShakeFrequency,
            _player.PlayerData.attackShakeDuration
        );
    }

    /// <summary>
    /// 抖动镜头
    /// </summary>
    /// <param name="intensity">振幅</param>
    /// <param name="frequency">频率</param>
    /// <param name="time">持续时间</param>
    public void Shake(float intensity, float frequency, float time)
    {
        if (_perlin == null || time <= 0f) return;

        // 只停止上一次抖动协程，不影响帧冻结协程，防止多个抖动冲突
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, frequency, time));
    }

    /// <summary>
    /// 执行抖动协程，结束后清零噪声并强制回正镜头。
    /// </summary>
    /// <param name="intensity">抖动振幅。</param>
    /// <param name="frequency">抖动频率。</param>
    /// <param name="time">抖动持续时间。</param>
    private IEnumerator ShakeRoutine(float intensity, float frequency, float time)
    {
        _perlin.m_AmplitudeGain = intensity;
        _perlin.m_FrequencyGain = frequency;

        yield return new WaitForSecondsRealtime(time);

        _perlin.m_AmplitudeGain = 0;
        _perlin.m_FrequencyGain = 0;
        ResetCameraTilt();
    }

    /// <summary>
    /// 帧冻结
    /// </summary>
    /// <param name="duration">持续时间</param>
    public void HitStop(float duration)
    {
        if (duration <= 0f) return;

        // 首次进入帧冻结时记录原始 timeScale，连续命中只延长冻结，不覆盖原始值。
        if (!_isHitStopping)
        {
            _timeScaleBeforeHitStop = Time.timeScale > 0f ? Time.timeScale : 1f;
            _isHitStopping = true;
        }

        // 只停止上一次帧冻结协程，不影响抖动协程
        if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
        _hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }

    /// <summary>
    /// 执行帧冻结协程，到时后恢复冻结前的 timeScale。
    /// </summary>
    /// <param name="duration">冻结时长（真实时间）。</param>
    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = _timeScaleBeforeHitStop;
        _isHitStopping = false;
        _hitStopCoroutine = null;
    }

    /// <summary>
    /// 在组件禁用或销毁时，兜底恢复 timeScale，防止游戏卡在冻结状态。
    /// </summary>
    private void RestoreTimeScaleIfNeeded()
    {
        if (!_isHitStopping) return;

        Time.timeScale = _timeScaleBeforeHitStop;
        _isHitStopping = false;
        _hitStopCoroutine = null;
    }

    /// <summary>
    /// 重置镜头倾斜和局部旋转，防止抖动后残留角度。
    /// </summary>
    private void ResetCameraTilt()
    {
        if (playerCamera == null) return;

        var lens = playerCamera.m_Lens;
        lens.Dutch = _defaultDutch;
        playerCamera.m_Lens = lens;

        playerCamera.transform.localRotation = _defaultVirtualCameraLocalRotation;
        if (_perlin != null)
        {
            _perlin.transform.localRotation = _defaultNoiseNodeLocalRotation;
        }
    }
}
