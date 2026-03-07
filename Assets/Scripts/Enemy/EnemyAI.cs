using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人AI控制器，使用状态机模式
/// </summary>
public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("敌人数据")]
    public EnemyBaseData enemyBaseData;
    private Health health;

    [Header("AI设置")]
    [Tooltip("巡逻速度")]
    public float PatrolSpeed = 2f;
    [Tooltip("追逐速度")]
    public float ChaseSpeed = 3f;
    [Tooltip("巡逻等待时间")]
    public float PatrolWaitTime = 2f;
    [Tooltip("巡逻点1")]
    public Transform PatrolPoint1;
    [Tooltip("巡逻点2")]
    public Transform PatrolPoint2;
    [Tooltip("追逐范围")]
    public float ChaseRange = 5f;
    [Tooltip("攻击范围")]
    public float AttackRange = 1.5f;
    [Tooltip("攻击力")]
    public float AttackDamage = 10f;
    [Tooltip("攻击速度")]
    public float AttackRate = 1f;
    [Tooltip("攻击冲击力")]
    public float AttackImpactForce = 5f;

    [Header("硬直设置")]
    [Tooltip("触发硬直的最小冲击力")]
    public float MinImpactForHurt = 10f;
    [Tooltip("硬直持续时间")]
    public float HurtDuration = 0.5f;
    [Tooltip("硬直冲击力倍率")]
    public float ImpactToDurationMultiplier = 0.02f;

    // 状态机
    public StateMachine StateMachine { get; private set; }

    // 动画控制器
    public Animator Animator { get; private set; }

    private Rigidbody2D rb;

    void Start()
    {
        // 初始化组件
        health = GetComponent<Health>();
        health.maxHealth = enemyBaseData.maxHealth;
        health.currentHealth = health.maxHealth;

        rb = GetComponent<Rigidbody2D>();
        Animator = GetComponent<Animator>();

        // 初始化状态机
        StateMachine = new StateMachine();
        StateMachine.ChangeState(new PatrolState(this));
    }

    void Update()
    {
        StateMachine.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.FixedUpdate();
    }

    /// <summary>
    /// 敌人受到伤害
    /// </summary>
    /// <param name="rawDamage">原始伤害</param>
    /// <param name="impactForce">冲击力</param>
    public void TakeDamage(float rawDamage, float impactForce = 0f)
    {
        // 计算实际伤害
        float finalDamage = Mathf.Max(rawDamage - enemyBaseData.defence, 0);
        health.UpdateHealth(finalDamage);

        // 检查是否应该触发硬直
        if (impactForce >= MinImpactForHurt)
        {
            TriggerHurtState(impactForce);
        }
    }

    /// <summary>
    /// 触发硬直状态
    /// </summary>
    /// <param name="impactForce">冲击力</param>
    private void TriggerHurtState(float impactForce)
    {
        // 根据冲击力计算硬直时间
        float hurtDuration = HurtDuration + (impactForce - MinImpactForHurt) * ImpactToDurationMultiplier;

        // 获取当前状态
        IEnemyState currentState = StateMachine.GetCurrentState();

        // 切换到硬直状态
        StateMachine.ChangeState(new HurtState(this, hurtDuration, currentState));
    }
}