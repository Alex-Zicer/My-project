using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class HUDManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup hudCanvasGroup;

    /// <summary>
    /// 显示或者隐藏整个UI（观看剧情或者进入主菜单时隐藏）
    /// </summary>
    /// <param name="isActive">显示或者隐藏UI</param>
    public void SetHUDActive(bool isActive)
    {
        if(hudCanvasGroup != null)
        {
        hudCanvasGroup.alpha = isActive ? 1 : 0;
        hudCanvasGroup.blocksRaycasts = isActive;
        hudCanvasGroup.interactable = isActive;
        }
    }
}
