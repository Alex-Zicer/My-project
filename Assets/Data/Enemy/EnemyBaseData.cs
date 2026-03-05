using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyBaseData : ScriptableObject
{
    [Header("基本属性")]
    public string enemyName; //敌人名称
    public float maxHealth; //最大生命值
    public float moveSpeed; //移动速度

    [Header("攻击属性")]
    public float attackDamage; //攻击伤害
    public float attackRange; //攻击范围
    public float attackCooldown; //攻击冷却时间
}
