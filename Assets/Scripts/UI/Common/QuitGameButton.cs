using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class QuitGameButton : MonoBehaviour
{
    public void QuitGame()
    {
        // 1. 如果是在 Unity 编辑器里运行，点击后停止播放模式
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
            // 2. 如果是正式发布的版本，执行退出程序
            Application.Quit();
#endif

        Debug.Log("游戏已退出");
    }
}
