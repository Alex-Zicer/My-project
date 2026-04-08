using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using UnityEngine;

/// <summary>
/// 背包存档组件：仅存储数据层（itemId + count），恢复后由背包系统触发 UI 刷新。
/// </summary>
public class InventorySaveable : SaveableBehaviour
{
    private const int MinItemCount = 1; // 物品数量下限。
    private const int MinStackSize = 1; // 堆叠上限最小值。
    private const int DefaultStackSize = 99; // 缺省堆叠上限。
    private const string DefaultDatabasePath = "Inventory/ItemDatabase"; // Resources 路径。

    // 背包引用（为空时自动查找 Inventory.Instance）。
    [SerializeField] private Inventory _inventory;

    // 物品数据库资源路径（Resources 下）。
    [SerializeField] private string _databaseResourcePath = DefaultDatabasePath;

    // 运行时数据库缓存。
    private ItemDatabaseSO _itemDatabase;

    [Serializable]
    private class InventoryState
    {
        // 物品状态列表。
        public List<InventoryItemState> items = new List<InventoryItemState>();
    }

    [Serializable]
    private class InventoryItemState
    {
        // 物品唯一 ID。
        public string itemId;

        // 该物品总数量。
        public int count;
    }

    /// <summary>
    /// Unity 生命周期：缓存依赖后注册到 SaveManager。
    /// </summary>
    protected override void Awake()
    {
        // 若当前组件挂在重复的 Inventory 副本上，则跳过注册。
        // 典型场景：返回主菜单后，场景内又创建了一份 Inventory，而真正生效的是常驻单例 Inventory.Instance。
        Inventory localInventory = GetComponent<Inventory>();
        if (localInventory != null && Inventory.Instance != null && Inventory.Instance != localInventory)
        {
            return;
        }

        FindInventoryIfNeeded();
        base.Awake();
    }

    /// <summary>
    /// 捕获背包状态（itemId + count）。
    /// </summary>
    /// <returns>背包状态对象。</returns>
    public override object CaptureState()
    {
        FindInventoryIfNeeded();

        InventoryState state = new InventoryState();
        if (_inventory == null)
        {
            Debug.LogWarning("[InventorySaveable] CaptureState 失败：Inventory 引用为空。");
            return state;
        }

        Dictionary<string, int> totalCountByItemId = new Dictionary<string, int>();
        IReadOnlyList<InventoryItem> allItems = _inventory.Items;

        for (int index = 0; index < allItems.Count; index++)
        {
            InventoryItem inventoryItem = allItems[index];
            if (inventoryItem == null || inventoryItem.ItemData == null)
            {
                continue;
            }

            string itemId = inventoryItem.ItemData.itemId;
            if (string.IsNullOrWhiteSpace(itemId))
            {
                Debug.LogWarning($"[InventorySaveable] 物品 itemId 为空，已跳过：{inventoryItem.ItemData.name}");
                continue;
            }

            int itemCount = Mathf.Max(inventoryItem.Count, MinItemCount);
            if (totalCountByItemId.TryGetValue(itemId, out int currentCount))
            {
                totalCountByItemId[itemId] = currentCount + itemCount;
            }
            else
            {
                totalCountByItemId[itemId] = itemCount;
            }
        }

        foreach (KeyValuePair<string, int> pair in totalCountByItemId)
        {
            state.items.Add(new InventoryItemState
            {
                itemId = pair.Key,
                count = pair.Value
            });
        }

        return state;
    }

    /// <summary>
    /// 恢复背包状态（itemId + count）。
    /// </summary>
    /// <param name="state">背包状态对象。</param>
    public override void RestoreState(object state)
    {
        FindInventoryIfNeeded();
        if (_inventory == null)
        {
            Debug.LogWarning("[InventorySaveable] RestoreState 失败：Inventory 引用为空。");
            return;
        }

        InventoryState inventoryState = ConvertState<InventoryState>(state);
        if (inventoryState == null)
        {
            Debug.LogWarning("[InventorySaveable] RestoreState 失败：状态数据为空或格式不正确。");
            return;
        }

        ItemDatabaseSO database = GetItemDatabase();
        Dictionary<string, ItemDataBase> fallbackItemById = BuildFallbackItemLookup();

        if (database == null)
        {
            Debug.LogWarning("[InventorySaveable] 未加载到 ItemDatabaseSO，将尝试使用运行时回退映射恢复背包。");
        }

        List<InventoryItem> restoredItems = new List<InventoryItem>();
        List<InventoryItemState> savedItems = inventoryState.items ?? new List<InventoryItemState>();

        for (int index = 0; index < savedItems.Count; index++)
        {
            InventoryItemState savedItem = savedItems[index];
            if (savedItem == null || string.IsNullOrWhiteSpace(savedItem.itemId))
            {
                continue;
            }

            int count = Mathf.Max(savedItem.count, MinItemCount);
            ItemDataBase itemData = ResolveItemData(savedItem.itemId, database, fallbackItemById);
            if (itemData == null)
            {
                Debug.LogWarning($"[InventorySaveable] 找不到 itemId={savedItem.itemId} 对应的物品，已跳过。");
                continue;
            }

            AddRestoredItems(restoredItems, itemData, count);
        }

        _inventory.ReplaceAllItems(restoredItems);

        if (savedItems.Count > 0 && restoredItems.Count == 0)
        {
            Debug.LogWarning("[InventorySaveable] 读档完成但未恢复任何背包物品，请检查 ItemDatabase 或物品 itemId 配置。");
        }
    }

    /// <summary>
    /// 根据物品堆叠规则把恢复数据展开为 InventoryItem 列表。
    /// </summary>
    /// <param name="targetList">目标列表。</param>
    /// <param name="itemData">物品数据。</param>
    /// <param name="count">总数量。</param>
    private static void AddRestoredItems(List<InventoryItem> targetList, ItemDataBase itemData, int count)
    {
        if (targetList == null || itemData == null)
        {
            return;
        }

        int safeCount = Mathf.Max(count, MinItemCount);

        if (!itemData.isStackable)
        {
            for (int index = 0; index < safeCount; index++)
            {
                targetList.Add(new InventoryItem(itemData, MinItemCount));
            }

            return;
        }

        int maxStackSize = itemData.maxStackSize > MinStackSize ? itemData.maxStackSize : DefaultStackSize;
        int remaining = safeCount;
        while (remaining > 0)
        {
            int stackCount = Mathf.Min(remaining, maxStackSize);
            targetList.Add(new InventoryItem(itemData, stackCount));
            remaining -= stackCount;
        }
    }

    /// <summary>
    /// 延迟查找 Inventory 引用。
    /// </summary>
    private void FindInventoryIfNeeded()
    {
        if (_inventory != null)
        {
            return;
        }

        _inventory = Inventory.Instance;
        if (_inventory != null)
        {
            return;
        }

#if UNITY_2023_1_OR_NEWER
        _inventory = FindFirstObjectByType<Inventory>();
#else
        _inventory = FindObjectOfType<Inventory>();
#endif
    }

    /// <summary>
    /// 获取运行时物品数据库。
    /// </summary>
    /// <returns>ItemDatabaseSO 实例。</returns>
    private ItemDatabaseSO GetItemDatabase()
    {
        if (_itemDatabase != null)
        {
            return _itemDatabase;
        }

        string resourcePath = string.IsNullOrWhiteSpace(_databaseResourcePath) ? DefaultDatabasePath : _databaseResourcePath;
        _itemDatabase = Resources.Load<ItemDatabaseSO>(resourcePath);
        if (_itemDatabase == null)
        {
            Debug.LogWarning($"[InventorySaveable] Resources.Load 失败，路径={resourcePath}");
        }

        return _itemDatabase;
    }

    /// <summary>
    /// 通过数据库与回退映射解析 itemId 对应的物品数据。
    /// </summary>
    /// <param name="itemId">物品唯一 ID。</param>
    /// <param name="database">运行时数据库。</param>
    /// <param name="fallbackItemById">回退映射。</param>
    /// <returns>匹配到的物品数据；找不到返回 null。</returns>
    private static ItemDataBase ResolveItemData(
        string itemId,
        ItemDatabaseSO database,
        Dictionary<string, ItemDataBase> fallbackItemById)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        if (database != null)
        {
            ItemDataBase databaseItem = database.GetItemById(itemId);
            if (databaseItem != null)
            {
                return databaseItem;
            }
        }

        if (fallbackItemById != null && fallbackItemById.TryGetValue(itemId, out ItemDataBase fallbackItem))
        {
            return fallbackItem;
        }

        return null;
    }

    /// <summary>
    /// 构建运行时 itemId 映射（用于数据库缺失时的回退恢复）。
    /// 数据来源：当前背包 + 当前场景 ItemPickup。
    /// </summary>
    /// <returns>itemId 到 ItemDataBase 的映射表。</returns>
    private Dictionary<string, ItemDataBase> BuildFallbackItemLookup()
    {
        Dictionary<string, ItemDataBase> itemById = new Dictionary<string, ItemDataBase>();

        if (_inventory != null)
        {
            IReadOnlyList<InventoryItem> inventoryItems = _inventory.Items;
            for (int index = 0; index < inventoryItems.Count; index++)
            {
                InventoryItem inventoryItem = inventoryItems[index];
                if (inventoryItem == null)
                {
                    continue;
                }

                AddItemToLookup(itemById, inventoryItem.ItemData);
            }
        }

#if UNITY_2023_1_OR_NEWER
        ItemPickup[] pickups = FindObjectsByType<ItemPickup>(FindObjectsInactive.Include, FindObjectsSortMode.None);
#else
        ItemPickup[] pickups = FindObjectsOfType<ItemPickup>(true);
#endif
        for (int index = 0; index < pickups.Length; index++)
        {
            ItemPickup pickup = pickups[index];
            if (pickup == null)
            {
                continue;
            }

            AddItemToLookup(itemById, pickup.ItemData);
        }

        return itemById;
    }

    /// <summary>
    /// 把单个物品加入映射表。
    /// </summary>
    /// <param name="itemById">目标映射。</param>
    /// <param name="itemData">物品数据。</param>
    private static void AddItemToLookup(Dictionary<string, ItemDataBase> itemById, ItemDataBase itemData)
    {
        if (itemById == null || itemData == null)
        {
            return;
        }

        if (string.IsNullOrWhiteSpace(itemData.itemId))
        {
            return;
        }

        itemById[itemData.itemId] = itemData;
    }

    /// <summary>
    /// 将 object 状态安全转换为目标类型。
    /// </summary>
    /// <typeparam name="T">目标状态类型。</typeparam>
    /// <param name="state">原始状态对象。</param>
    /// <returns>转换后的状态对象。</returns>
    private static T ConvertState<T>(object state) where T : class
    {
        if (state == null)
        {
            return null;
        }

        if (state is T typed)
        {
            return typed;
        }

        if (state is JObject jObject)
        {
            return jObject.ToObject<T>();
        }

        if (state is JToken jToken)
        {
            return jToken.ToObject<T>();
        }

        if (state is string jsonString)
        {
            return JsonConvert.DeserializeObject<T>(jsonString);
        }

        string fallbackJson = JsonConvert.SerializeObject(state);
        return JsonConvert.DeserializeObject<T>(fallbackJson);
    }
}
