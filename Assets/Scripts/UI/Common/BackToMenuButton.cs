using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BackToMenuButton : MonoBehaviour
{
    public void BackMenu()
    {
        // 调用SceneLoader加载
        Time.timeScale = 1f;
        SceneLoader.Instance.LoadScene("MainMenu");
    }
}
