using UnityEngine;

/// <summary>
/// 杂物数据 SO。继承 ItemDataBase 获得通用物品信息，
/// 并添加杂物专属字段（售价等）。用于材料、消耗品等不属于武器/防具的物品。
/// </summary>
[CreateAssetMenu(fileName = "NewMiscData", menuName = "Data/MiscData")]
public class MiscData : ItemDataBase
{
    [Header("杂物专属")]
    [Tooltip("售卖价格")]
    public int sellPrice;
}
