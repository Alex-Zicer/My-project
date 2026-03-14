using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary> 页面分类枚举，用于被不同管理器自动分组。 </summary>
public enum UIPageCategory
{
    MainMenu,
    InGame,
    SubPage,
    HUD
}


/// <summary>
/// 通用 UI 页面组件。每个需要被页面管理器控制的界面都应挂载此脚本，并设置 Category 与 PageKey。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class UIPage : MonoBehaviour
{
    [Header("页面标识")]
    [Tooltip("页面分类，用于被不同管理器自动分组。")]
    [SerializeField] private UIPageCategory category = UIPageCategory.MainMenu;
    [Tooltip("页面唯一 Key,用于字典查找与跳转。为空时使用对象名。")]
    [SerializeField] private string pageKey;

    [Tooltip("打开页面时希望默认选中的 UI 对象（如第一个按钮），用于手柄/键盘导航。")]
    public GameObject defaultSelected;
    private CanvasGroup _canvasGroup;

    public UIPageCategory Category => category;
    public string PageKey => string.IsNullOrWhiteSpace(pageKey) ? gameObject.name : pageKey;

    /// <summary>
    /// 懒加载 CanvasGroup，保证显示/隐藏与射线检测可用。
    /// </summary>
    private CanvasGroup CanvasGroup
    {
        get
        {
            if (_canvasGroup == null)
            {
                _canvasGroup = GetComponent<CanvasGroup>();
            }
            return _canvasGroup;
        }
    }

    /// <summary>
    /// 将 EventSystem 的当前选中项设为 defaultSelected。在页面打开时由管理器调用，便于手柄/键盘聚焦。
    /// </summary>
    public void SetSelectedUIToDefault()
    {
        if (defaultSelected != null && UIManager.Instance != null && UIManager.Instance.eventSystem != null)
        {
            UIManager.Instance.eventSystem.SetSelectedGameObject(null);
            UIManager.Instance.eventSystem.SetSelectedGameObject(defaultSelected);
        }
    }

    /// <summary>
    /// 打开页面：激活物体并设置 CanvasGroup 为可交互、可射线检测、不透明。
    /// </summary>
    public void Open()
    {
        gameObject.SetActive(true);
        CanvasGroup.alpha = 1.0f;
        CanvasGroup.blocksRaycasts = true;
        CanvasGroup.interactable = true;
    }

    /// <summary>
    /// 关闭页面：隐藏物体并关闭交互与射线检测。
    /// </summary>
    public void Close()
    {
        gameObject.SetActive(false);
        CanvasGroup.alpha = 0;
        CanvasGroup.blocksRaycasts = false;
        CanvasGroup.interactable = false;
    }
}
