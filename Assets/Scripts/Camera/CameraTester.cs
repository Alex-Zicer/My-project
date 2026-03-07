using UnityEngine;

public class CameraTester : MonoBehaviour
{
    public CameraController cameraController;

    void Update()
    {
        // 按下 J 键测试抖动
        if (Input.GetKeyDown(KeyCode.J))
        {
            cameraController.TriggerShake();
            Debug.Log("触发抖动！");
        }

        // 按下 K 键测试帧冻结（打击感）
        if (Input.GetKeyDown(KeyCode.K))
        {
            cameraController.TriggerFreeze();
            Debug.Log("触发帧冻结！");
        }

        // 同时触发（模拟强力打击效果）
        if (Input.GetKeyDown(KeyCode.L))
        {
            cameraController.TriggerShake();
            cameraController.TriggerFreeze();
        }
    }
}