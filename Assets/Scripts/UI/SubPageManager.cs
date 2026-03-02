using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SubPageManager : MonoBehaviour
{
    [Header("子页面列表")]
    public List<GameObject> subPages; // 子页面列表

    private void OnEnable()
    {
        ShowSubPage(0); // 默认显示第一个子页面
    }

    /// <summary>
    /// 激活指定索引的子页面，并隐藏其他所有子页面的方法。
    /// 通过调用此方法，可以在UI中切换不同的子页面，确保用户界面的一致性和易用性。
    /// </summary>
    /// <param name="index">切换的子页面的索引</param>
    public void ShowSubPage(int index)
    {
        for (int i = 0; i < subPages.Count; i++)
        {
            subPages[i].SetActive(i == index); // 仅激活指定索引的子页面，其他页面隐藏
        }        
    }
}
