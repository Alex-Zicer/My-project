using UnityEngine;

/// <summary>
/// 所有物品的 SO 基类。存储物品共有的通用信息（名称、图标、描述、分类等），
/// 具体物品类型（武器、防具、杂物）继承此基类并添加各自专属字段。
/// </summary>
public enum ItemType
{
    Weapon,
    Armor,
    Misc
}

public abstract class ItemDataBase : ScriptableObject
{
    [Header("通用信息")]
    [Tooltip("物品名称")]
    public string itemName;
    [Tooltip("物品图标，用于背包格子显示")]
    public Sprite icon;
    [Tooltip("物品描述")]
    [TextArea] public string description;
    [Tooltip("物品分类")]
    public ItemType itemType;
    [Tooltip("是否可叠加（武器/防具通常不可，材料/杂物可以）")]
    public bool isStackable;
}
