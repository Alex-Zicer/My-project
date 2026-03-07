using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    [Header("CineMachine设置")]
    [Tooltip("CineMachine虚拟相机")]
    public CinemachineVirtualCamera virtualCamera;
    [Tooltip("目标跟随对象")]
    public Transform target;

    [Header("Impulse抖动设置")]
    [Tooltip("Impulse源")]
    public CinemachineImpulseSource impulseSource;
    [Tooltip("抖动力度")]
    [Range(0.1f, 10f)]
    public float impulseForce = 1f;
    [Tooltip("抖动方向")]
    public Vector3 shakeDirection = new Vector3(0.5f, 0.5f, 0f);

    [Header("帧冻结设置")]
    [Tooltip("冻结持续时间")]
    public float freezeDuration = 0.1f;
    [Tooltip("冻结时的时间缩放")]
    public float freezeTimeScale = 0.1f;
    private bool isFrozen = false;

    private void Start()
    {
        // 确保ImpulseSource组件存在，但不自动触发
        if (impulseSource == null)
        {
            impulseSource = GetComponent<CinemachineImpulseSource>();
        }

        // 如果没有ImpulseSource，不自动添加，需要在Inspector中手动配置
        // 这样可以避免初始化时的意外抖动
    }

    /// <summary>
    /// 触发镜头抖动
    /// </summary>
    public void TriggerShake()
    {
        if (impulseSource != null)
        {
            // 使用Impulse系统触发抖动
            impulseSource.GenerateImpulse(shakeDirection * impulseForce);
        }
        else
        {
            Debug.LogWarning("ImpulseSource未配置，无法触发镜头抖动");
        }
    }

    /// <summary>
    /// 触发帧冻结
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
        isFrozen = true;
        float originalTimeScale = Time.timeScale;
        Time.timeScale = freezeTimeScale;
        yield return new WaitForSeconds(freezeDuration * freezeTimeScale);
        Time.timeScale = originalTimeScale;
        isFrozen = false;
    }
}