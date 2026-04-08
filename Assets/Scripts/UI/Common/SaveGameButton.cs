using UnityEngine;

/// <summary>
/// 挂在场景对象上的存档按钮逻辑，供 Button.OnClick 直接绑定。
/// </summary>
public class SaveGameButton : MonoBehaviour
{
    private const int MinSlotIndex = 0; // 槽位下限。

    [Header("存档设置")]
    [SerializeField] private int _slotIndex = 0; // 保存槽位编号。
    [SerializeField] private bool _enableClickLog = true; // 是否输出点击日志。

    /// <summary>
    /// 按钮点击后触发：保存到配置槽位。
    /// </summary>
    public void SaveGame()
    {
        int safeSlotIndex = Mathf.Max(_slotIndex, MinSlotIndex);
        _ = SaveManager.Instance.Save(safeSlotIndex);

        if (_enableClickLog)
        {
            Debug.Log($"[SaveGameButton] 已触发保存，槽位={safeSlotIndex}");
        }
    }

    /// <summary>
    /// 按钮点击后触发：从配置槽位加载。
    /// </summary>
    public void LoadGame()
    {
        int safeSlotIndex = Mathf.Max(_slotIndex, MinSlotIndex);
        _ = SaveManager.Instance.Load(safeSlotIndex);

        if (_enableClickLog)
        {
            Debug.Log($"[SaveGameButton] 已触发加载，槽位={safeSlotIndex}");
        }
    }
}
