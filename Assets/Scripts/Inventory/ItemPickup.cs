using UnityEngine;

/// <summary>
/// 场景中的可拾取物品。挂在掉落物 GameObject 上，需要带一个设为 IsTrigger 的 Collider2D。
/// 玩家碰到后自动拾取进背包并销毁掉落物。
/// itemData 字段拖入具体的 SO 文件（WeaponData / ArmorData / MiscData 均可，因为它们都继承 ItemDataBase）。
/// </summary>
public class ItemPickup : MonoBehaviour
{
    [Header("掉落物配置")]
    [Tooltip("拖入物品的 SO 文件（WeaponData / ArmorData / MiscData 等）")]
    [SerializeField] private ItemDataBase itemData;
    [Tooltip("拾取数量，不可叠加物品此值通常为 1")]
    [SerializeField] private int amount = 1;

    /// <summary>
    /// 当玩家进入触发器范围时，将物品添加到背包并销毁掉落物。
    /// </summary>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (itemData == null)
        {
            Debug.LogWarning("掉落物未配置 itemData，无法拾取。");
            return;
        }
        if (amount <= 0)
        {
            Debug.LogWarning($"掉落物 {itemData.itemName} 的数量无效，无法拾取。");
            return;
        }

        if (Inventory.Instance == null)
        {
            Debug.LogWarning("Inventory 单例不存在，无法拾取物品。");
            return;
        }

        bool added = Inventory.Instance.AddItem(itemData, amount);
        if (!added)
        {
            Debug.LogWarning($"拾取 {itemData.itemName} 失败，掉落物不会被销毁。");
            return;
        }

        Debug.Log($"拾取了 {itemData.itemName} x{amount}");
        Destroy(gameObject);
    }
}
