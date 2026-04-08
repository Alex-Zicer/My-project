using UnityEngine;

/// <summary>
/// 场景中的可拾取物品组件。挂在掉落物 GameObject 上，需要配合一个设为 IsTrigger 的 Collider2D 使用。
/// 玩家碰到触发器后，自动将物品添加进背包并销毁该掉落物 GameObject。
/// itemData 字段在 Inspector 中拖入具体的 SO 文件（WeaponData / ArmorData / MiscData 均可，
/// 因为它们都继承自 ItemDataBase）。
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("掉落物配置")]
    [Tooltip("拖入物品的 SO 文件（WeaponData / ArmorData / MiscData 等）")]
    [SerializeField] private ItemDataBase itemData;

    [Tooltip("拾取数量，不可叠加物品此值通常为 1")]
    [SerializeField] private int amount = 1;

    /// <summary>
    /// 只读访问掉落物对应的物品数据（供存档系统回退映射使用）。
    /// </summary>
    public ItemDataBase ItemData => itemData;

    /// <summary>
    /// Unity 物理回调：当带有 Collider2D 的对象进入本触发器时执行。
    /// 只响应标签为 "Player" 的对象，完成拾取逻辑后销毁自身。
    /// </summary>
    /// <param name="other">进入触发器的碰撞体</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        // 只有玩家才能触发拾取，其他碰撞体（敌人、子弹等）直接忽略
        if (!other.CompareTag("Player")) return;

        // 未配置物品数据时，打印警告并中止（避免静默失败）
        if (itemData == null)
        {
            Debug.LogWarning("掉落物未配置 itemData，无法拾取。");
            return;
        }

        // 数量非法时同样中止
        if (amount <= 0)
        {
            Debug.LogWarning($"掉落物 {itemData.itemName} 的数量无效，无法拾取。");
            return;
        }

        // 确保背包单例存在（场景中必须有挂载 Inventory 组件的 GameObject）
        if (Inventory.Instance == null)
        {
            Debug.LogWarning("Inventory 单例不存在，无法拾取物品。");
            return;
        }

        // 尝试将物品加入背包
        bool added = Inventory.Instance.AddItem(itemData, amount);
        if (!added)
        {
            // 添加失败时不销毁掉落物，保留在场景中供玩家再次尝试
            Debug.LogWarning($"拾取 {itemData.itemName} 失败，掉落物不会被销毁。");
            return;
        }

        Debug.Log($"拾取了 {itemData.itemName} x{amount}");
        // 拾取成功后销毁掉落物 GameObject，从场景中移除
        Destroy(gameObject);
    }
}
