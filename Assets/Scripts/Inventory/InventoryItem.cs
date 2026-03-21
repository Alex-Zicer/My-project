using System;
using UnityEngine;

/// <summary>
/// 背包中的运行时物品实例。
/// itemData 指向静态 SO 模板（共享数据），instanceId / count 等字段保存该条目的运行时状态。
/// 注意：同一种物品可以有多个 InventoryItem 条目（不可叠加时），每个条目有独立的 instanceId。
/// </summary>
[Serializable]
public class InventoryItem
{
    // 运行时唯一标识符（GUID），用于精确定位某一具体实例（如装备、丢弃特定一把武器）
    [SerializeField] private string instanceId;

    // 指向 ScriptableObject 模板，存储物品名称、图标、类型等静态数据
    [SerializeField] private ItemDataBase itemData;

    // 当前堆叠数量（不可叠加物品固定为 1）
    [SerializeField] private int count;

    // 只读属性，外部通过属性访问，防止直接修改字段
    public string InstanceId => instanceId;
    public ItemDataBase ItemData => itemData;
    public int Count => count;

    // 是否可叠加：由 SO 数据决定，itemData 为空时视为不可叠加
    public bool IsStackable => itemData != null && itemData.isStackable;

    /// <summary>
    /// 构造函数：创建一个新的背包物品条目。
    /// </summary>
    /// <param name="itemData">物品的 SO 模板数据，不能为 null</param>
    /// <param name="count">初始数量，最小为 1</param>
    /// <param name="instanceId">可选：指定实例 ID；为空时自动生成 GUID</param>
    public InventoryItem(ItemDataBase itemData, int count = 1, string instanceId = null)
    {
        this.itemData = itemData;
        // 数量最小为 1，防止创建出数量为 0 的无效条目
        this.count = Mathf.Max(1, count);
        // 未传入 instanceId 时，自动生成一个不含连字符的 GUID 作为唯一标识
        this.instanceId = string.IsNullOrWhiteSpace(instanceId)
            ? Guid.NewGuid().ToString("N")
            : instanceId;
    }

    /// <summary>
    /// 判断本条目是否可以与指定物品数据叠加。
    /// 条件：本条目可叠加 且 物品 SO 引用相同（同一种物品）。
    /// </summary>
    /// <param name="otherItemData">要判断的物品 SO</param>
    /// <returns>可叠加返回 true</returns>
    public bool CanStackWith(ItemDataBase otherItemData)
    {
        // 必须同时满足：本条目标记为可叠加 + 是同一个 SO 实例（同种物品）
        return IsStackable && itemData == otherItemData;
    }

    /// <summary>
    /// 增加物品数量（拾取、购买时调用）。
    /// </summary>
    /// <param name="amount">要增加的数量，必须大于 0，否则忽略</param>
    public void AddCount(int amount)
    {
        if (amount <= 0) return;
        count += amount;
    }

    /// <summary>
    /// 减少物品数量（消耗、出售时调用）。
    /// 数量最低降到 0，不会出现负数；调用方需在 count 归零后自行决定是否移除该条目。
    /// </summary>
    /// <param name="amount">要减少的数量，必须大于 0，否则忽略</param>
    public void RemoveCount(int amount)
    {
        if (amount <= 0) return;
        // Mathf.Max 保证 count 不低于 0
        count = Mathf.Max(0, count - amount);
    }
}
