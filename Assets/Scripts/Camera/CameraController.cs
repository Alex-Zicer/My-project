using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

/// <summary>
/// 相机反馈控制器。
/// 负责统一封装镜头抖动（Impulse）与短时帧冻结（Freeze Frame）两类打击反馈。
/// 设计意图：让战斗或受击系统只调用公开方法，不直接耦合 Cinemachine 细节。
/// </summary>
public class CameraController : MonoBehaviour
{
    /// <summary>
    /// Cinemachine 相关引用：
    /// 虚拟相机和目标对象通常由场景或其他系统配置。
    /// </summary>
    [Header("CineMachine 设置")]
    [Tooltip("CineMachine 虚拟相机")]
    public CinemachineVirtualCamera virtualCamera;
    [Tooltip("目标跟随对象")]
    public Transform target;

    /// <summary>
    /// Impulse 抖动参数：
    /// impulseSource 决定抖动信号来源；
    /// impulseForce 与 shakeDirection 共同决定抖动强度和方向。
    /// </summary>
    [Header("Impulse 抖动设置")]
    [Tooltip("Impulse 源")]
    public CinemachineImpulseSource impulseSource;
    [Tooltip("抖动力度")]
    [Range(0.1f, 10f)]
    public float impulseForce = 1f;
    [Tooltip("抖动方向")]
    public Vector3 shakeDirection = new Vector3(0.5f, 0.5f, 0f);

    /// <summary>
    /// 帧冻结参数：
    /// freezeDuration 为体感时长；
    /// freezeTimeScale 为冻结期间 Time.timeScale，通常取较小值而非 0。
    /// </summary>
    [Header("帧冻结设置")]
    [Tooltip("冻结持续时间")]
    public float freezeDuration = 0.1f;
    [Tooltip("冻结时的时间缩放")]
    public float freezeTimeScale = 0.1f;

    // 防止重复启动冻结协程。
    private bool isFrozen = false;

    private void Start()
    {
        // 运行时兜底：若未在 Inspector 指定，则尝试从同物体自动获取。
        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        // 未找到时不自动 AddComponent，避免在错误对象上引入意外镜头行为。
    }

    /// <summary>
    /// 触发镜头抖动。
    /// 常在攻击命中、受击、爆炸等时机调用。
    /// </summary>
    public void TriggerShake()
    {
        if (impulseSource != null)
        {
            // 通过 Impulse 系统发射一段冲击信号，Cinemachine 会根据通道配置响应。
            impulseSource.GenerateImpulse(shakeDirection * impulseForce);
        }
        else
        {
            Debug.LogWarning("ImpulseSource未配置，无法触发镜头抖动");
        }
    }

    /// <summary>
    /// 触发帧冻结。
    /// 在冻结尚未结束时重复调用会被忽略，避免叠加导致时间缩放异常。
    /// </summary>
    public void TriggerFreeze()
    {
        if (!isFrozen)
        {
            StartCoroutine(FreezeFrame());
        }
    }

    private IEnumerator FreezeFrame()
    {
        // 记录原时间缩放，结束后恢复，保证不会污染外部时间控制逻辑。
        isFrozen = true;
        float originalTimeScale = Time.timeScale;
        Time.timeScale = freezeTimeScale;

        // WaitForSeconds 受 timeScale 影响，因此这里乘以 freezeTimeScale，
        // 让“体感冻结时长”接近 freezeDuration。
        yield return new WaitForSeconds(freezeDuration * freezeTimeScale);
        Time.timeScale = originalTimeScale;
        isFrozen = false;
    }
}
