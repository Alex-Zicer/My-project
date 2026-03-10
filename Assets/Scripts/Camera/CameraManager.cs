using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraManager : MonoBehaviour
{
    public static CameraManager Instance { get; private set; }

    [SerializeField] private CinemachineVirtualCamera playerCamera;
    //用来访问noise,处理镜头抖动
    private CinemachineBasicMultiChannelPerlin _perlin;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        // 获取 Noise 模块的引用
        _perlin = playerCamera.GetCinemachineComponent<CinemachineBasicMultiChannelPerlin>();
    }

    /// <summary>
    /// 抖动镜头
    /// </summary>
    /// <param name="intensity">振幅</param>
    /// <param name="frequency">频率</param>
    /// <param name="time">时间</param>
    public void Shake(float intensity, float frequency, float time)
    {
        StopAllCoroutines();//防止多个抖动冲突
        StartCoroutine(ShakeRoutine(intensity, frequency, time));
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
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1;
    }
}
