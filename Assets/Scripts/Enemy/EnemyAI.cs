using UnityEngine;

/// <summary>
/// 敌人 AI 主体。
/// 负责缓存公共组件、初始化状态机，并提供给各状态共享的数据与方法。
/// </summary>
public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("数据")]
    public EnemyBaseData data;

    [Header("巡逻点")]
    public Transform patrolPoint1;
    public Transform patrolPoint2;

    // 缓存引用（Awake 初始化）
    public Rigidbody2D Rb { get; private set; }
    public Animator Anim { get; private set; }
    public Transform PlayerTransform { get; private set; }

    public EnemyStateMachine StateMachine { get; private set; }

    private Health _health;
    private bool _hasAttackedOnce;
    private float _nextAttackReadyTime;
    private const float MinAttackRate = 0.01f;

    // EnemyBaseData 属性代理
    public float PatrolSpeed => data.patrolSpeed;
    public float PatrolWaitTime => data.patrolWaitTime;
    public float ChaseSpeed => data.chaseSpeed;
    public float ChaseRange => data.chaseRange;
    public float AttackRange => data.attackRange;
    public float AttackExitRange => data.attackExitRange;
    public float AttackDamage => data.attackDamage;
    public float AttackRate => data.attackRate;
    public AudioEventSO AttackHitSfxEvent => data != null ? data.attackHitSfxEvent : null;
    public float HurtDuration => data.hurtDuration;
    public float KnockbackForce => data.knockbackForce;
    public Transform PatrolPoint1 => patrolPoint1;
    public Transform PatrolPoint2 => patrolPoint2;

    // 首刀直接可打；后续需等冷却
    public bool CanAttackNow => !_hasAttackedOnce || Time.time >= _nextAttackReadyTime;

    /// <summary>
    /// 获取组件引用并初始化状态。
    /// </summary>
    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        Anim = GetComponentInChildren<Animator>();
        PlayerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        // 防止巡逻点跟随敌人移动，保持世界坐标稳定
        if (patrolPoint1 != null) patrolPoint1.SetParent(null);
        if (patrolPoint2 != null) patrolPoint2.SetParent(null);

        _health = GetComponent<Health>();
        _health.maxHealth = data.maxHealth;
        _health.currentHealth = data.maxHealth;

        InitializeStateMachine();
    }

    private void InitializeStateMachine()
    {
        StateMachine = new EnemyStateMachine();

        // 预实例化全部状态，避免运行时频繁分配
        StateMachine.RegisterState(new PatrolState(this));
        StateMachine.RegisterState(new ChaseState(this));
        StateMachine.RegisterState(new AttackState(this));
        StateMachine.RegisterState(new HurtState(this));
        StateMachine.RegisterState(new DeadState(this));

        StateMachine.Initialize(EnemyStateType.Patrol);
    }

    private void Update() => StateMachine.Update();
    private void FixedUpdate() => StateMachine.FixedUpdate();

    /// <summary>
    /// 记录一次攻击完成，并按 attackRate 开始冷却。
    /// 冷却信息保存在 EnemyAI 上，可跨状态切换保留。
    /// </summary>
    public void MarkAttackPerformed()
    {
        float attackRate = data != null ? data.attackRate : 0f;
        if (attackRate <= 0f)
        {
            Debug.LogWarning(
                $"[EnemyAI] {name} 的 attackRate 非法（{attackRate}），将使用最小值 {MinAttackRate}。",
                this);
            attackRate = MinAttackRate;
        }

        _hasAttackedOnce = true;
        _nextAttackReadyTime = Time.time + (1f / attackRate);
    }

    public void TakeDamage(float rawDamage)
    {
        if (StateMachine.CurrentStateType == EnemyStateType.Dead) return;

        float finalDamage = Mathf.Max(0, rawDamage - data.defence);
        _health.UpdateHealth(finalDamage);

        if (_health.currentHealth <= 0)
        {
            StateMachine.TransitionTo(EnemyStateType.Dead);
        }
        else
        {
            // 记录受击前状态，供 Hurt 恢复时切回
            StateMachine.GetState<HurtState>()?.SetPreviousState(StateMachine.CurrentStateType);
            StateMachine.TransitionTo(EnemyStateType.Hurt);
        }
    }
}
