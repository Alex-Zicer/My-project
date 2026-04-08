using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 物品数据库：维护 itemId 与 ItemDataBase 的双向查询缓存。
/// </summary>
[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Inventory/ItemDatabase")]
public class ItemDatabaseSO : ScriptableObject
{
    // 物品资源列表（由编辑器工具重建）。
    [SerializeField] private List<ItemDataBase> items = new List<ItemDataBase>();

    // itemId -> ItemDataBase 查询表。
    private readonly Dictionary<string, ItemDataBase> _itemById = new Dictionary<string, ItemDataBase>();

    // ItemDataBase -> itemId 查询表。
    private readonly Dictionary<ItemDataBase, string> _idByItem = new Dictionary<ItemDataBase, string>();

    // 查询缓存是否已构建。
    private bool _cacheBuilt;

    /// <summary>
    /// 只读访问全部物品列表。
    /// </summary>
    public IReadOnlyList<ItemDataBase> Items => items;

    private void OnEnable()
    {
        InvalidateCache();
    }

    /// <summary>
    /// 按 itemId 获取物品配置。
    /// </summary>
    /// <param name="itemId">物品唯一 ID。</param>
    /// <returns>匹配的物品 SO，不存在返回 null。</returns>
    public ItemDataBase GetItemById(string itemId)
    {
        if (string.IsNullOrWhiteSpace(itemId))
        {
            return null;
        }

        BuildCacheIfNeeded();
        return _itemById.TryGetValue(itemId, out ItemDataBase itemData) ? itemData : null;
    }

    /// <summary>
    /// 按物品 SO 获取 itemId。
    /// </summary>
    /// <param name="itemData">物品 SO。</param>
    /// <returns>物品 ID，不存在返回空字符串。</returns>
    public string GetItemId(ItemDataBase itemData)
    {
        if (itemData == null)
        {
            return string.Empty;
        }

        BuildCacheIfNeeded();
        return _idByItem.TryGetValue(itemData, out string itemId) ? itemId : string.Empty;
    }

    /// <summary>
    /// 编辑器调用：替换数据库物品列表。
    /// </summary>
    /// <param name="newItems">新的物品列表。</param>
    public void SetItems(List<ItemDataBase> newItems)
    {
        items = newItems != null ? new List<ItemDataBase>(newItems) : new List<ItemDataBase>();
        InvalidateCache();
    }

    /// <summary>
    /// 清空缓存，下次查询时重建。
    /// </summary>
    public void InvalidateCache()
    {
        _cacheBuilt = false;
        _itemById.Clear();
        _idByItem.Clear();
    }

    /// <summary>
    /// 按需构建缓存。
    /// </summary>
    private void BuildCacheIfNeeded()
    {
        if (_cacheBuilt)
        {
            return;
        }

        _itemById.Clear();
        _idByItem.Clear();

        for (int index = 0; index < items.Count; index++)
        {
            ItemDataBase itemData = items[index];
            if (itemData == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(itemData.itemId))
            {
                Debug.LogWarning($"[ItemDatabaseSO] 物品 itemId 为空，已忽略：{itemData.name}");
                continue;
            }

            if (_itemById.ContainsKey(itemData.itemId))
            {
                Debug.LogWarning($"[ItemDatabaseSO] 发现重复 itemId，后者将覆盖前者：{itemData.itemId}");
            }

            _itemById[itemData.itemId] = itemData;
            _idByItem[itemData] = itemData.itemId;
        }

        _cacheBuilt = true;
    }
}
