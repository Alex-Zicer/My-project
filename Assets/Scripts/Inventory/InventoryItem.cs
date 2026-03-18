using System;
using UnityEngine;

/// <summary>
/// 背包中的运行时物品实例。
/// itemData 指向静态 SO 模板，instanceId / count 等字段保存运行时状态。
/// </summary>
[Serializable]
public class InventoryItem
{
    [SerializeField] private string instanceId;
    [SerializeField] private ItemDataBase itemData;
    [SerializeField] private int count;

    public string InstanceId => instanceId;
    public ItemDataBase ItemData => itemData;
    public int Count => count;
    public bool IsStackable => itemData != null && itemData.isStackable;

    public InventoryItem(ItemDataBase itemData, int count = 1, string instanceId = null)
    {
        this.itemData = itemData;
        this.count = Mathf.Max(1, count);
        this.instanceId = string.IsNullOrWhiteSpace(instanceId)
            ? Guid.NewGuid().ToString("N")
            : instanceId;
    }

    public bool CanStackWith(ItemDataBase otherItemData)
    {
        return IsStackable && itemData == otherItemData;
    }

    public void AddCount(int amount)
    {
        if (amount <= 0) return;
        count += amount;
    }

    public void RemoveCount(int amount)
    {
        if (amount <= 0) return;
        count = Mathf.Max(0, count - amount);
    }
}
