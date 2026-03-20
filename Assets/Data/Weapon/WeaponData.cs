using UnityEngine;

/// <summary>
/// 武器数据 SO。继承 ItemDataBase 获得通用物品信息（名称、图标、描述等），
/// 并添加武器专属字段（连击段数、每段攻击数据、动画前缀）。
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Data/WeaponData")]
public class WeaponData : ItemDataBase
{
    private void OnEnable()
    {
        itemType = ItemType.Weapon;
        isStackable = false;
    }

    [Header("武器专属")]
    public int attackComboCount;

    [Header("每段攻击数据")]
    public AttackData[] attackData;

    [Header("动画")]
    public string attackAnimationPrefix;


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
