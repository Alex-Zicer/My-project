using UnityEngine;
using UnityEngine.Serialization;

/// <summary>
/// 武器数据 SO：在通用物品信息基础上，扩展战斗相关配置。
/// </summary>
[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Data/WeaponData")]
public class WeaponData : ItemDataBase
{
    // 连击段数上限。
    [Header("武器专属")]
    public int attackComboCount;

    // 每段攻击的数据配置。
    [Header("每段攻击数据")]
    public AttackData[] attackData;

    // 攻击动画前缀。
    [Header("动画")]
    public string attackAnimationPrefix;

    /// <summary>
    /// 资源启用时确保武器类型与堆叠规则正确。
    /// </summary>
    private void OnEnable()
    {
        itemType = ItemType.Weapon;
        isStackable = false;
    }

    /// <summary>
    /// 获取武器基础属性描述文本。
    /// </summary>
    /// <returns>用于 UI 展示的属性字符串。</returns>
    public override string GetStatsText() => $"攻击力：{attackData?[0].damage ?? 0}";
}

/// <summary>
/// 单段攻击的数据描述。
/// </summary>
[System.Serializable]
public class AttackData
{
    // 伤害数值。
    public float damage;

    // 本段攻击时长（秒）。
    public float duration;

    // 命中窗口开始时间（0~1）。
    [Range(0f, 1f)] public float hitStartTime;

    // 命中窗口结束时间（0~1）。
    [Range(0f, 1f)] public float hitEndTime;

    // 命中检测半径。
    public float attackRange;

    // 命中检测偏移。
    public Vector2 attackOffset;

    // 本段攻击命中后的结果音效事件（可选，留空不会报错）。
    // 当前动作挥击音效建议通过动画关键帧事件触发。
    [FormerlySerializedAs("attackSfxEvent")]
    [FormerlySerializedAs("attackSound")]
    public AudioEventSO hitSfxEvent;
}
