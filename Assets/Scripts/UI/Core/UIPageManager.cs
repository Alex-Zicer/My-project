using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using Unity.VisualScripting;
using UnityEngine;

public class UIPageManager : MonoBehaviour
{
    [Header("页面管理")]
    [Tooltip("所有页面")]
    [SerializeField] private List<UIPage> pageList;
    [Tooltip("管理页面栈")]
    private Stack<UIPage> historyStack = new Stack<UIPage>();
    public bool IsPause {  get; private set; }
    /// <summary>
    /// 开始关闭所有页面
    /// </summary>
    public void Initialize()
    {
        foreach (var page in pageList)
        {
            if(page == null)
            {
                Debug.LogError("pageList中有空页面");
            }
            else
            {
                page.Close();
            }
        }
        GoToPageByName("MainMenuPage");
    }

    /// <summary>
    /// 通过页面名称切换页面的方法，接受一个字符串参数pageName，表示要切换到的页面名称。
    /// </summary>
    /// <param name="pageName">切换页面的名称</param>
    public void GoToPageByName(string pageName)
    {
        UIPage target = pageList.Find(page => page.gameObject.name == pageName);//在页面列表中查找与给定名称匹配的页面
        if (target == null)
        {
            Debug.Log($"未找到名为{pageName}的页面");
            return;
        }

        //如果当前有页面在显示，隐藏当前页面，但是保留在栈内
        if (historyStack.Count > 0)
        {
            historyStack.Peek().Close();
        }
        //打开对应页面，并推入栈内
        target.Open();
        historyStack.Push(target);

        target.SetSelectedUIToDefault();
    }

    /// <summary>
    /// 返回上一页
    /// </summary>
    public void Back()
    {
        if (historyStack.Count <= 1) return;//如果当前栈内只有一个页面，那就是当前正在显示的页面

        //关闭当前页面
        UIPage currentPage = historyStack.Pop();
        currentPage.Close();

        //显示上一个页面
        UIPage previousPage = historyStack.Peek();
        previousPage.Open();
    }

    /// <summary>
    /// 关闭所有页面，并清除栈
    /// </summary>
    public void CloseAll()
    {
        while(historyStack.Count > 0)
        {
            historyStack.Pop().Close();
        }
    }

    #region 暂停功能

    /// <summary>
    /// 外部入口
    /// </summary>
    public void TogglePause()
    {
        if (!IsPause)
        {
            PauseGame();
        }
        else
        {
            ResumeGame();
        }
    }

    /// <summary>
    /// 冻结时间，并打开暂停页面
    /// </summary>
    private void PauseGame()
    {
        IsPause = true;
        Time.timeScale = 0;
        GoToPageByName("PausePage");
    }

    /// <summary>
    /// 恢复时间，关闭暂停页面
    /// </summary>
    public void ResumeGame()
    {
        IsPause = false;
        Time.timeScale = 1;

        Back();//返回上一页，也就是关闭暂停页
    }
    #endregion
}
