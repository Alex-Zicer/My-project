using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 挂在场景对象上的存档按钮逻辑，供 Button.OnClick 直接绑定。
/// </summary>
public class SaveGameButton : MonoBehaviour
{
    private const int MinSlotIndex = 0; // 槽位下限。
    private const string GamePlaySceneName = "GamePlay"; // 游戏场景名。

    [Header("存档设置")]
    [SerializeField] private int _slotIndex = 0; // 保存槽位编号。
    [SerializeField] private bool _enableClickLog = true; // 是否输出点击日志。
    [SerializeField] private bool _onlyLoadInGamePlay = true; // 是否限制“加载”只能在 GamePlay 场景触发。

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
        if (_onlyLoadInGamePlay && SceneManager.GetActiveScene().name != GamePlaySceneName)
        {
            Debug.LogWarning($"[SaveGameButton] 当前场景不是 {GamePlaySceneName}，已阻止直接调用 LoadGame。请使用主菜单读档入口。");
            return;
        }

        int safeSlotIndex = Mathf.Max(_slotIndex, MinSlotIndex);
        _ = SaveManager.Instance.Load(safeSlotIndex);

        if (_enableClickLog)
        {
            Debug.Log($"[SaveGameButton] 已触发加载，槽位={safeSlotIndex}");
        }
    }
}
