using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public enum PanelType
{
    Default,
    Pop,
    Tab
}

[RequireComponent(typeof(CanvasGroup))]
public class UIPage : MonoBehaviour
{
    [Tooltip("页面类型")]
    public PanelType type;
    [Tooltip("默认选中的UI选项")]
    public GameObject defaultSelected;
    private CanvasGroup _canvasGroup;

    /// <summary>
    /// 初始化每个页面的CanvasGroup，确保在执行时CanvasGroup不为空
    /// </summary>
    private CanvasGroup CanvasGroup
    {
        get
        {
            if(_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
            return _canvasGroup;
        }
    }

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

    public void Open()
    {
        gameObject.SetActive(true);
        CanvasGroup.alpha = 1.0f;
        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.interactable = true;    
    }

    public void Close()
    {
        gameObject.SetActive(false);
        CanvasGroup.alpha = 0;
        CanvasGroup.blocksRaycasts = false;
        CanvasGroup.interactable = false;
    }
}


