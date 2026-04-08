using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

/// <summary>
/// 物品数据库构建工具：扫描全部 ItemDataBase 并生成 ItemDatabase 资源。
/// </summary>
public static class ItemDatabaseBuilder
{
    private const string DatabaseAssetPath = "Assets/Resources/Inventory/ItemDatabase.asset"; // 数据库资产路径。
    private const string ResourceRoot = "Assets/Resources"; // Resources 根目录。
    private const string InventoryFolder = "Assets/Resources/Inventory"; // Inventory 目录。

    /// <summary>
    /// 菜单命令：重建 ItemDatabase 资源，并补齐缺失/重复 itemId。
    /// </summary>
    [MenuItem("Tools/Inventory/重建 ItemDatabase")]
    public static void RebuildDatabase()
    {
        string[] itemGuids = AssetDatabase.FindAssets("t:ItemDataBase");
        List<ItemDataBase> allItems = new List<ItemDataBase>(itemGuids.Length);
        HashSet<string> usedItemIds = new HashSet<string>();
        int generatedCount = 0;
        int fixedDuplicateCount = 0;

        for (int index = 0; index < itemGuids.Length; index++)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(itemGuids[index]);
            ItemDataBase itemData = AssetDatabase.LoadAssetAtPath<ItemDataBase>(assetPath);
            if (itemData == null)
            {
                continue;
            }

            if (string.IsNullOrWhiteSpace(itemData.itemId))
            {
                itemData.GenerateNewItemId();
                generatedCount++;
            }

            while (!string.IsNullOrWhiteSpace(itemData.itemId) && usedItemIds.Contains(itemData.itemId))
            {
                itemData.GenerateNewItemId();
                fixedDuplicateCount++;
            }

            if (string.IsNullOrWhiteSpace(itemData.itemId))
            {
                Debug.LogWarning($"[ItemDatabaseBuilder] 物品 ID 为空，已跳过：{assetPath}");
                continue;
            }

            usedItemIds.Add(itemData.itemId);
            EditorUtility.SetDirty(itemData);
            allItems.Add(itemData);
        }

        allItems.Sort((left, right) =>
        {
            string leftName = left != null ? left.itemName : string.Empty;
            string rightName = right != null ? right.itemName : string.Empty;
            return string.Compare(leftName, rightName, System.StringComparison.Ordinal);
        });

        ItemDatabaseSO database = LoadOrCreateDatabaseAsset();
        database.SetItems(allItems);
        EditorUtility.SetDirty(database);

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();

        Debug.Log($"[ItemDatabaseBuilder] 重建完成：共 {allItems.Count} 个物品，补齐 ID {generatedCount} 个，修复重复 ID {fixedDuplicateCount} 个。");
    }

    /// <summary>
    /// 读取或创建 ItemDatabase 资源。
    /// </summary>
    /// <returns>可用的 ItemDatabaseSO 实例。</returns>
    private static ItemDatabaseSO LoadOrCreateDatabaseAsset()
    {
        ItemDatabaseSO database = AssetDatabase.LoadAssetAtPath<ItemDatabaseSO>(DatabaseAssetPath);
        if (database != null)
        {
            return database;
        }

        if (!AssetDatabase.IsValidFolder(ResourceRoot))
        {
            AssetDatabase.CreateFolder("Assets", "Resources");
        }

        if (!AssetDatabase.IsValidFolder(InventoryFolder))
        {
            AssetDatabase.CreateFolder(ResourceRoot, "Inventory");
        }

        database = ScriptableObject.CreateInstance<ItemDatabaseSO>();
        AssetDatabase.CreateAsset(database, DatabaseAssetPath);
        AssetDatabase.SaveAssets();
        return database;
    }
}
