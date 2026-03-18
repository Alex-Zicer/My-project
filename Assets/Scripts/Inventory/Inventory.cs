using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包管理器（独立单例）。负责存储玩家拥有的所有物品，并提供添加、移除、按类型筛选等操作。
/// 当背包内容发生变化时会触发 OnInventoryChanged 事件，供 UI 等外部系统订阅刷新。
/// </summary>
public class Inventory : MonoBehaviour
{
    public static Inventory Instance { get; private set; }

    [Header("背包数据")]
    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

    /// <summary> 背包内容变化时触发，UI 可订阅此事件来刷新列表。 </summary>
    public event Action OnInventoryChanged;

    /// <summary> 只读访问当前背包所有物品。 </summary>
    public IReadOnlyList<InventoryItem> Items => items;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 向背包添加物品。可叠加物品会累加数量，不可叠加物品会新增独立条目。
    /// </summary>
    /// <param name="itemData">要添加的物品 SO 数据</param>
    /// <param name="amount">添加数量，默认为 1</param>
    public bool AddItem(ItemDataBase itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return false;

        if (itemData.isStackable)
        {
            // 可叠加：查找已有条目并累加数量
            InventoryItem existing = items.Find(i => i.CanStackWith(itemData));
            if (existing != null)
            {
                existing.AddCount(amount);
            }
            else
            {
                items.Add(new InventoryItem(itemData, amount));
            }
        }
        else
        {
            // 不可叠加：每个都作为独立条目（如武器、防具）
            for (int i = 0; i < amount; i++)
            {
                items.Add(new InventoryItem(itemData, 1));
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 从背包移除物品。可叠加物品会减少数量（归零则移除条目），不可叠加物品直接移除条目。
    /// </summary>
    /// <param name="itemData">要移除的物品 SO 数据</param>
    /// <param name="amount">移除数量，默认为 1</param>
    public bool RemoveItem(ItemDataBase itemData, int amount = 1)
    {
        if (itemData == null || amount <= 0) return false;
        if (GetItemCount(itemData) < amount) return false;

        if (itemData.isStackable)
        {
            InventoryItem existing = items.Find(i => i.CanStackWith(itemData));
            if (existing == null) return false;

            existing.RemoveCount(amount);
            if (existing.Count <= 0)
            {
                items.Remove(existing);
            }
        }
        else
        {
            int removed = 0;
            for (int i = items.Count - 1; i >= 0 && removed < amount; i--)
            {
                if (items[i].ItemData != itemData) continue;

                items.RemoveAt(i);
                removed++;
            }
        }

        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 按运行时实例 ID 移除一个物品条目，用于未来装备/丢弃等需要精确定位实例的场景。
    /// </summary>
    public bool RemoveItemByInstanceId(string instanceId)
    {
        if (string.IsNullOrWhiteSpace(instanceId)) return false;

        InventoryItem existing = items.Find(i => i.InstanceId == instanceId);
        if (existing == null) return false;

        items.Remove(existing);
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 按物品类型筛选背包中的物品，用于背包 UI 的分类按钮。
    /// </summary>
    /// <param name="type">筛选的物品类型</param>
    /// <returns>符合该类型的物品列表</returns>
    public List<InventoryItem> GetItemsByType(ItemType type)
    {
        return items.FindAll(i => i.ItemData != null && i.ItemData.itemType == type);
    }

    /// <summary>
    /// 获取所有物品（不筛选），用于"全部"分类。
    /// </summary>
    /// <returns>背包中全部物品</returns>
    public List<InventoryItem> GetAllItems()
    {
        return new List<InventoryItem>(items);
    }

    /// <summary>
    /// 检查背包中是否包含指定物品。
    /// </summary>
    /// <param name="itemData">要检查的物品</param>
    /// <returns>包含则返回 true</returns>
    public bool HasItem(ItemDataBase itemData)
    {
        return GetItemCount(itemData) > 0;
    }

    /// <summary>
    /// 获取指定物品在背包中的数量。
    /// </summary>
    /// <param name="itemData">要查询的物品</param>
    /// <returns>该物品总数量，不存在则返回 0</returns>
    public int GetItemCount(ItemDataBase itemData)
    {
        if (itemData == null) return 0;

        int totalCount = 0;
        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];
            if (item.ItemData != itemData) continue;

            totalCount += item.Count;
        }

        return totalCount;
    }
}
