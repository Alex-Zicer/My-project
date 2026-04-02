using UnityEngine;

/// <summary>
/// 相机反馈调试器。
/// 在编辑调试阶段通过快捷键快速触发相机抖动与帧冻结，验证打击感参数。
/// </summary>
public class CameraTester : MonoBehaviour
{
    [Header("调试参数")]
    public float shakeIntensity = 2f;
    public float shakeFrequency = 4f;
    public float shakeDuration = 0.15f;
    public float hitStopDuration = 0.05f;

    private void Update()
    {
        if (CameraManager.Instance == null) return;

        // 按 J：仅触发抖动。
        if (Input.GetKeyDown(KeyCode.J))
        {
            CameraManager.Instance.Shake(shakeIntensity, shakeFrequency, shakeDuration);
            Debug.Log("触发抖动。");
        }

        // 按 K：仅触发帧冻结（常用于观察停顿感）。
        if (Input.GetKeyDown(KeyCode.K))
        {
            CameraManager.Instance.HitStop(hitStopDuration);
            Debug.Log("触发帧冻结。");
        }

        // 按 L：同时触发抖动 + 冻结，模拟重击反馈。
        if (Input.GetKeyDown(KeyCode.L))
        {
            CameraManager.Instance.Shake(shakeIntensity, shakeFrequency, shakeDuration);
            CameraManager.Instance.HitStop(hitStopDuration);
            Debug.Log("触发组合反馈（抖动 + 冻结）。");
        }
    }
}
