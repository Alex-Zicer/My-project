using UnityEngine;

[CreateAssetMenu(fileName = "NewEnemyData", menuName = "Data/EnemyData")]
public class EnemyBaseData : ScriptableObject
{
    [Header("Base")]
    public string enemyName;
    public float maxHealth;
    public float defence;

    [Header("Patrol")]
    public float patrolSpeed = 2f;
    public float patrolWaitTime = 1f;

    [Header("Chase")]
    public float chaseSpeed = 4f;
    public float chaseRange = 8f;

    [Header("Attack")]
    public float attackDamage = 10f;
    public float attackRange = 1.5f;

    [Tooltip("Should be larger than attackRange to avoid boundary state thrashing.")]
    public float attackExitRange = 2.5f;

    public float attackRate = 1f;

    [Header("Audio")]
    public AudioEventSO attackHitSfxEvent;

    [Header("Hurt")]
    public float hurtDuration = 0.76f;
    public float knockbackForce = 0.1f;
}
