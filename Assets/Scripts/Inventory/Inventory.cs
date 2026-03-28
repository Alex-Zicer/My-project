using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 背包管理器（独立单例）。负责存储玩家拥有的所有物品，并提供添加、移除、按类型筛选等操作。
/// 当背包内容发生变化时会触发 OnInventoryChanged 事件，供 UI 等外部系统订阅刷新。
/// </summary>
public class Inventory : MonoBehaviour
{
    // 单例实例，全局唯一，外部只读
    public static Inventory Instance { get; private set; }

    [Header("背包数据")]
    // 背包中所有物品的运行时列表，序列化以便在 Inspector 中调试查看
    [SerializeField] private List<InventoryItem> items = new List<InventoryItem>();

    /// <summary> 背包内容变化时触发，UI 可订阅此事件来刷新列表。 </summary>
    public event Action OnInventoryChanged;

    /// <summary> 只读访问当前背包所有物品，防止外部直接修改列表。 </summary>
    public IReadOnlyList<InventoryItem> Items => items;

    /// <summary>
    /// Unity 生命周期：对象激活时执行单例初始化。
    /// 若场景中已存在另一个 Inventory 实例，则销毁自身（防止重复）；
    /// 否则将自身注册为单例，并标记为跨场景不销毁。
    /// </summary>
    private void Awake()
    {
        // 已存在其他实例时，销毁自身避免重复
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // 切换场景时保留背包数据
        // 子对象挂在已常驻根节点下时无需再次调用，避免 Unity 警告。
        if (transform.parent == null)
        if (transform.parent == null)
        {
            DontDestroyOnLoad(gameObject);
        }
    }

    /// <summary>
    /// 向背包添加物品。可叠加物品会累加数量，不可叠加物品会新增独立条目。
    /// </summary>
    /// <param name="itemData">要添加的物品 SO 数据</param>
    /// <param name="amount">添加数量，默认为 1</param>
    /// <returns>添加成功返回 true，参数非法返回 false</returns>
    public bool AddItem(ItemDataBase itemData, int amount = 1)
    {
        // 参数校验：itemData 不能为空，数量必须大于 0
        if (itemData == null || amount <= 0) return false;

        if (itemData.isStackable)
        {
            int remaining = amount;
            int maxStack = itemData.maxStackSize > 0 ? itemData.maxStackSize : 99;

            // 先填满已有的未满条目
            foreach (var item in items)
            {
                if (remaining <= 0) break;
                if (!item.CanStackWith(itemData)) continue;
                int space = maxStack - item.Count;
                if (space <= 0) continue;
                int add = Mathf.Min(space, remaining);
                item.AddCount(add);
                remaining -= add;
            }

            // 剩余数量开新条目
            while (remaining > 0)
            {
                int add = Mathf.Min(maxStack, remaining);
                items.Add(new InventoryItem(itemData, add));
                remaining -= add;
            }
        }
        else
        {
            // 不可叠加物品（如武器、防具）：每一个都作为独立条目存入
            for (int i = 0; i < amount; i++)
            {
                items.Add(new InventoryItem(itemData, 1));
            }
        }

        // 通知所有订阅者（如背包 UI）刷新显示
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 从背包移除物品。可叠加物品会减少数量（归零则移除条目），不可叠加物品直接移除条目。
    /// </summary>
    /// <param name="itemData">要移除的物品 SO 数据</param>
    /// <param name="amount">移除数量，默认为 1</param>
    /// <returns>移除成功返回 true，数量不足或参数非法返回 false</returns>
    public bool RemoveItem(ItemDataBase itemData, int amount = 1)
    {
        // 参数校验
        if (itemData == null || amount <= 0) return false;
        // 背包中该物品总数不足时，拒绝移除（防止出现负数）
        if (GetItemCount(itemData) < amount) return false;

        if (itemData.isStackable)
        {
            // 可叠加物品：找到对应条目并减少数量
            InventoryItem existing = items.Find(i => i.CanStackWith(itemData));
            if (existing == null) return false;

            existing.RemoveCount(amount);
            // 数量归零时，从列表中彻底移除该条目
            if (existing.Count <= 0)
            {
                items.Remove(existing);
            }
        }
        else
        {
            // 不可叠加物品：从列表末尾往前遍历，逐个移除（倒序避免索引错位）
            int removed = 0;
            for (int i = items.Count - 1; i >= 0 && removed < amount; i--)
            {
                if (items[i].ItemData != itemData) continue;

                items.RemoveAt(i);
                removed++;
            }
        }

        // 通知订阅者刷新 UI
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 按运行时实例 ID 移除一个物品条目。
    /// 适用于装备、丢弃等需要精确定位某一具体实例的场景（同类物品有多个时不会误删）。
    /// </summary>
    /// <param name="instanceId">目标物品的唯一实例 ID（由 InventoryItem 构造时生成的 GUID）</param>
    /// <returns>找到并移除成功返回 true，否则返回 false</returns>
    public bool RemoveItemByInstanceId(string instanceId)
    {
        // 空 ID 直接拒绝
        if (string.IsNullOrWhiteSpace(instanceId)) return false;

        // 按实例 ID 精确查找
        InventoryItem existing = items.Find(i => i.InstanceId == instanceId);
        if (existing == null) return false;

        items.Remove(existing);
        // 通知订阅者刷新 UI
        OnInventoryChanged?.Invoke();
        return true;
    }

    /// <summary>
    /// 按物品类型筛选背包中的物品，用于背包 UI 的分类按钮（武器/防具/消耗品等）。
    /// </summary>
    /// <param name="type">要筛选的物品类型枚举值</param>
    /// <returns>符合该类型的物品列表（新列表，不影响原始数据）</returns>
    public List<InventoryItem> GetItemsByType(ItemType type)
    {
        // FindAll 会返回新列表，外部修改不会影响 items
        return items.FindAll(i => i.ItemData != null && i.ItemData.itemType == type);
    }

    /// <summary>
    /// 获取背包中所有物品（不筛选），用于"全部"分类标签页。
    /// </summary>
    /// <returns>背包全部物品的副本列表</returns>
    public List<InventoryItem> GetAllItems()
    {
        // 返回副本，防止外部直接操作内部列表
        return new List<InventoryItem>(items);
    }

    /// <summary>
    /// 检查背包中是否包含至少 1 个指定物品。
    /// </summary>
    /// <param name="itemData">要检查的物品 SO</param>
    /// <returns>包含则返回 true</returns>
    public bool HasItem(ItemDataBase itemData)
    {
        return GetItemCount(itemData) > 0;
    }

    /// <summary>
    /// 对背包物品排序：先按 ItemType 枚举值升序（枚举顺序即优先级），同类再按物品名称升序。
    /// </summary>
    public void Sort()
    {
        items.Sort((a, b) =>
        {
            int typeCompare = ((int)a.ItemData.itemType).CompareTo((int)b.ItemData.itemType);
            if (typeCompare != 0) return typeCompare;
            return string.Compare(a.ItemData.itemName, b.ItemData.itemName, System.StringComparison.CurrentCulture);
        });
        OnInventoryChanged?.Invoke();
    }

    /// <summary>
    /// 获取指定物品在背包中的总数量。
    /// 对于不可叠加物品，每个独立条目计为 1，累加后返回总数。
    /// </summary>
    /// <param name="itemData">要查询的物品 SO</param>
    /// <returns>该物品总数量，不存在则返回 0</returns>
    public int GetItemCount(ItemDataBase itemData)
    {
        if (itemData == null) return 0;

        int totalCount = 0;
        for (int i = 0; i < items.Count; i++)
        {
            InventoryItem item = items[i];
            // 跳过不匹配的物品
            if (item.ItemData != itemData) continue;

            totalCount += item.Count;
        }

        return totalCount;
    }
}
