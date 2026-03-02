using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using UnityEngine.Events;

public class UIManager : MonoBehaviour
{
    //单例实例
    public static UIManager Instance;

    [Header("页面管理器")]
    [Tooltip("UI页面列表，包含所有需要管理的UI页面。")]
    public List<UIPage> pages;
    [Tooltip("当前激活的UI页面的索引号")]
    public int currentPageIndex = 0;
    [Tooltip("默认的UI页面索引号，游戏开始时会自动激活该页面。")]
    public int defaultPageIndex = 0;

    public EventSystem eventSystem;

    /// <summary>
    /// 切换页面的方法，接受一个整数参数pageIndex，表示要切换到的页面索引。
    /// </summary>
    /// <param name="pageIndex">要切换到的页面索引</param>
    public void GoToPage(int pageIndex)
    {
        if (pageIndex == currentPageIndex) return; // 如果要切换到的页面已经是当前页面，则不执行任何操作

        if (pages[currentPageIndex] != null)
        {
            pages[currentPageIndex].gameObject.SetActive(false);
            pages[pageIndex].gameObject.SetActive(true);
            pages[pageIndex].SetSelectedUIToDefault();
            currentPageIndex = pageIndex;// 更新当前页面索引
        }
    }

    /// <summary>
    /// 通过页面名称切换页面的方法，接受一个字符串参数pageName，表示要切换到的页面名称。
    /// </summary>
    /// <param name="pageName">切换页面的名称</param>
    public void GoToPageByName(string pageName)
    {
        UIPage page = pages.Find(pages => pages.gameObject.name == pageName);//在页面列表中查找与给定名称匹配的页面
        int pageIndex = pages.IndexOf(page);
        GoToPage(pageIndex);
    }


    /// <summary>
    /// 初始化事件系统，确保UI管理器能够正确处理用户输入和交互事件。
    /// </summary>
    private void SetUpEventSystem()
    {
        eventSystem = FindObjectOfType<EventSystem>();

        if (eventSystem == null)
        {
            Debug.LogWarning("缺少事件系统！");
        }
    }

    /// <summary>
    /// 初始化UI管理器，确保只有一个实例存在，并且在场景中保持不被销毁。
    /// 这种单例模式的实现方式可以确保在整个游戏生命周期中，UI管理器始终可用，
    /// 并且不会因为场景切换而丢失。
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else if(Instance != this)
        {
            Destroy(gameObject);
            return;
        }
    }

    /// <summary>
    /// 初始化UI管理器，设置事件系统和可交互UI元素的类型，以确保在游戏开始时，UI能够正确响应用户输入和交互事件。
    /// </summary>
    private void Start()
    {
        SetUpEventSystem();
        SetUpSelectablesType();        
    }

    /// <summary>
    /// 为场景中所有的可交互UI元素（如按钮、切换等）添加UIInteractableSound组件，
    /// 以便在用户与这些元素交互时播放相应的UI音效。
    /// </summary>
    private void SetUpSelectablesType()
    {
        //找到场景中所有的可交互UI元素（如按钮、切换等），
        //并为它们添加UIInteractableSound组件，以便在用户与这些元素交互时播放相应的UI音效。
        Selectable[] allSelects = Resources.FindObjectsOfTypeAll<Selectable>();

        foreach (var item in allSelects)
        {
            if (item is Button || item is Toggle)
            {
                //检查按钮上是否已经有UIButtonSound组件，
                //如果没有，则添加一个新的UIButtonSound组件，避免重复添加组件导致性能问题或者逻辑错误。
                if (item.GetComponent<UIInteractableSound>() == null)
                {
                    //如果按钮上没有UIButtonSound组件，则添加一个新的UIButtonSound组件
                    var soundScript = item.gameObject.AddComponent<UIInteractableSound>();

                    //根据按钮名称或者标签来设置UIButtonSound组件的soundType属性，
                    //以便在点击按钮时播放不同类型的UI音效
                    if (item.name.Contains("Close") || item.name.Contains("Back"))
                    {
                        soundScript.soundType = UISoundType.Back;
                    }
                    else if (item.name.Contains("Tab"))
                    {
                        soundScript.soundType = UISoundType.Tab;
                    }
                }
            }
        }
    }
}
