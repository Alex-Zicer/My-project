using UnityEngine;

/// <summary>
/// 防具数据 SO。继承 ItemDataBase 获得通用物品信息，
/// 并添加防具专属字段（防御加成、生命加成等）。
/// </summary>
[CreateAssetMenu(fileName = "NewArmorData", menuName = "Data/ArmorData")]
public class ArmorData : ItemDataBase
{
    private void OnEnable()
    {
        itemType = ItemType.Armor;
        isStackable = false;
    }

    [Header("防具专属")]
    [Tooltip("防御加成")]
    public float defenceBonus;
    [Tooltip("生命加成")]
    public float healthBonus;
}
