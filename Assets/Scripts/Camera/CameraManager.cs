using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private CinemachineVirtualCamera playerCamera;
    //用来访问noise，处理镜头抖动
    private CinemachineBasicMultiChannelPerlin _perlin;

    private PlayerController _player;
    // 分别持有协程引用，防止 Shake 和 HitStop 互相打断
    private Coroutine _shakeCoroutine;
    private Coroutine _hitStopCoroutine;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else Destroy(gameObject);

        // 获取 Noise 模块的引用
        _perlin = playerCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
        //将参数初始化为0，以免已进入游戏镜头就开始抖动
        _perlin.m_AmplitudeGain = 0;
        _perlin.m_FrequencyGain = 0;
    }

    private void Start()
    {
        // Start 里找 Player，确保 PlayerController.Awake 已执行完毕
        BindPlayer(FindObjectOfType<PlayerController>());
    }

    private void OnEnable()
    {
        // 场景切换后重新绑定新场景的 PlayerController（CameraManager 是 DontDestroyOnLoad）
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UnbindPlayer();
        BindPlayer(FindObjectOfType<PlayerController>());
    }

    private void BindPlayer(PlayerController player)
    {
        _player = player;
        if (_player == null) return;
        _player.OnHit       += OnPlayerHit;
        _player.OnAttackHit += OnAttackHit;
    }

    private void UnbindPlayer()
    {
        if (_player == null) return;
        _player.OnHit       -= OnPlayerHit;
        _player.OnAttackHit -= OnAttackHit;
        _player = null;
    }

    private void OnDestroy()
    {
        UnbindPlayer();
    }

    private void OnPlayerHit()
    {
        Shake(
            _player.PlayerData.hitShakeIntensity,
            _player.PlayerData.hitShakeFrequency,
            _player.PlayerData.hitShakeDuration
        );
    }

    private void OnAttackHit()
    {
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
        // 只停止上一次抖动协程，不影响帧冻结协程，防止多个抖动冲突
        if (_shakeCoroutine != null) StopCoroutine(_shakeCoroutine);
        _shakeCoroutine = StartCoroutine(ShakeRoutine(intensity, frequency, time));
    }

    private IEnumerator ShakeRoutine(float intensity, float frequency, float time)
    {
        _perlin.m_AmplitudeGain = intensity;
        _perlin.m_FrequencyGain = frequency;

        yield return new WaitForSecondsRealtime(time);

        _perlin.m_AmplitudeGain = 0;
        _perlin.m_FrequencyGain = 0;
    }

    /// <summary>
    /// 帧冻结
    /// </summary>
    /// <param name="duration">持续时间</param>
    public void HitStop(float duration)
    {
        // 只停止上一次帧冻结协程，不影响抖动协程
        if (_hitStopCoroutine != null) StopCoroutine(_hitStopCoroutine);
        _hitStopCoroutine = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }
}
