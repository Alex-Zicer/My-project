using UnityEngine;

/// <summary>
/// 武器数据 SO。继承 ItemDataBase 获得通用物品信息（名称、图标、描述等），
/// 并添加武器专属字段（连击段数、每段攻击数据、动画前缀）。
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Data/WeaponData")]
public class WeaponData : ItemDataBase
{
    [Header("武器专属")]
    public int attackComboCount;

    [Header("每段攻击数据")]
    public AttackData[] attackData;

    [Header("动画")]
    public string attackAnimationPrefix;

    /// <summary>
    /// 武器名称，映射自基类 itemName。
    /// 外部代码可继续使用 weaponName 访问，避免与基类 itemName 混淆。
    /// </summary>
    public string weaponName
    {
        get => itemName;
        set => itemName = value;
    }
}

[System.Serializable]
public class AttackData
{
    public float damage;
    public float duration;

    [Range(0f, 1f)] public float hitStartTime;
    [Range(0f, 1f)] public float hitEndTime;

    public float attackRange;
    public Vector2 attackOffset;
    public AudioClip attackSound;
}
