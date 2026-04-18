using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 游戏内常驻 HUD 管理器。控制血条、小地图等常驻 UI 的显隐（例如进主菜单时隐藏、进入游戏时显示）。
/// </summary>
public class HUDManager : MonoBehaviour
{
    [SerializeField] private CanvasGroup hudCanvasGroup; // HUD 根节点 CanvasGroup。
    public bool IsHUDActive { get; private set; } // 当前 HUD 显隐状态。

    public System.Action<bool> OnHUDActiveChanged; // HUD 显隐变化事件。

    /// <summary>
    /// 设置 HUD 整体显示或隐藏。用于进入主菜单/剧情时隐藏，进入游戏时显示。
    /// </summary>
    /// <param name="isActive">true 为显示，false 为隐藏（透明且不接收射线）</param>
    public void SetHUDActive(bool isActive)
    {
        if (hudCanvasGroup != null)
        {
            // 统一控制可见性、交互和射线响应。
            hudCanvasGroup.alpha = isActive ? 1 : 0;
            hudCanvasGroup.blocksRaycasts = isActive;
            hudCanvasGroup.interactable = isActive;
        }

        // 同步状态并广播给各 HUD 子模块（血条/能量/货币等）。
        IsHUDActive = isActive;
        OnHUDActiveChanged?.Invoke(isActive);
    }
}
