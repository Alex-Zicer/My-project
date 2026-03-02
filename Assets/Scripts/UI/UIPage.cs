using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIPage : MonoBehaviour
{
    [Tooltip("默认选中的UI选项")]
    public GameObject defaultSelected;

    /// <summary>
    /// 重新设置高亮UI为默认选项，通常在页面打开时调用，以确保用户界面的一致性和易用性。
    /// </summary>
    public void SetSelectedUIToDefault()
    {
        if(defaultSelected != null && UIManager.Instance != null)
        {
            UIManager.Instance.eventSystem.SetSelectedGameObject(null);
            UIManager.Instance.eventSystem.SetSelectedGameObject(defaultSelected);
        }
    }
}
