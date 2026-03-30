using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏内常驻 HUD 管理器。控制血条、小地图等常驻 UI 的显隐（例如进主菜单时隐藏、进入游戏时显示）。
/// </summary>
public class HUDManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup hudCanvasGroup;

    /// <summary>
    /// 设置 HUD 整体显示或隐藏。用于进入主菜单/剧情时隐藏，进入游戏时显示。
    /// </summary>
    /// <param name="isActive">true 为显示，false 为隐藏（透明且不接收射线）</param>
    public void SetHUDActive(bool isActive)
    {
        if (hudCanvasGroup != null)
        {
            hudCanvasGroup.alpha = isActive ? 1 : 0;
            hudCanvasGroup.blocksRaycasts = isActive;
            hudCanvasGroup.interactable = isActive;
        }
    }
}
