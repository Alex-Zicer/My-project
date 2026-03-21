using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Data/EnemyData")]
public class EnemyBaseData : ScriptableObject
{
    [Header("基本属性")]
    public string enemyName;
    public float maxHealth;
    public float defence;

    [Header("巡逻")]
    public float patrolSpeed = 2f;
    public float patrolWaitTime = 1f;   // 到达巡逻点后等待时间

    [Header("追击")]
    public float chaseSpeed = 4f;
    public float chaseRange = 8f;       // 进入追击的感知范围

    [Header("攻击")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f;    // 进入攻击的距离
    public float attackRate = 1f;       // 每秒攻击次数

    [Header("受伤")]
    public float hurtDuration = 0.3f;   // 受击硬直时长
}
