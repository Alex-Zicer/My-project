using UnityEngine;

/// <summary>
/// 挂在主菜单场景内的对象上，供 Button.OnClick 调用。
/// 这样按钮引用的是场景内对象，不会因为常驻单例销毁场景副本而丢失绑定。
/// </summary>
public class MainMenuButtonActions : MonoBehaviour
{
    private const int MinSlotIndex = 0; // 槽位下限。

    [Header("读档设置")]
    [SerializeField] private int _loadSlotIndex = 0; // 主菜单“加载游戏”使用的槽位。

    /// <summary>
    /// 进入游戏场景（不读档）。
    /// </summary>
    public void LoadGamePlay()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance 为空，无法加载 GamePlay。");
            return;
        }

        UIManager.Instance.LoadGamePlay();
    }

    /// <summary>
    /// 从主菜单进入游戏并读取指定槽位存档。
    /// </summary>
    public void LoadGamePlayFromSave()
    {
        if (UIManager.Instance == null)
        {
            Debug.LogWarning("UIManager.Instance 为空，无法执行主菜单读档。");
            return;
        }

        int safeSlotIndex = Mathf.Max(_loadSlotIndex, MinSlotIndex);
        UIManager.Instance.LoadGamePlayFromSave(safeSlotIndex);
    }
}
