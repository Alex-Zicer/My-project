using UnityEngine;

/// <summary>
/// 相机反馈调试器。
/// 在编辑调试阶段通过快捷键快速触发相机抖动与帧冻结，验证打击感参数。
/// </summary>
public class CameraTester : MonoBehaviour
{
    /// <summary>
    /// 目标相机控制器引用。
    /// 需要在 Inspector 中绑定到场景内的 CameraController。
    /// </summary>
    public CameraController cameraController;

    private void Update()
    {
        if (cameraController == null) return;

        // 按 J：仅触发抖动。
        if (Input.GetKeyDown(KeyCode.J))
        {
            cameraController.TriggerShake();
            Debug.Log("触发抖动。");
        }

        // 按 K：仅触发帧冻结（常用于观察停顿感）。
        if (Input.GetKeyDown(KeyCode.K))
        {
            cameraController.TriggerFreeze();
            Debug.Log("触发帧冻结。");
        }

        // 按 L：同时触发抖动 + 冻结，模拟重击反馈。
        if (Input.GetKeyDown(KeyCode.L))
        {
            cameraController.TriggerShake();
            cameraController.TriggerFreeze();
            Debug.Log("触发组合反馈（抖动 + 冻结）。");
        }
    }
}
