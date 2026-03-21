using UnityEngine;

/// <summary>
/// 敌人 AI 主体。持有状态机和所有共享引用，供各状态类读取。
/// 对齐玩家状态机设计：Awake 里预实例化并注册所有状态，无 GC。
/// </summary>
public class EnemyAI : MonoBehaviour, IDamageable
{
    [Header("数据")]
    public EnemyBaseData data;

    [Header("巡逻点")]
    public Transform patrolPoint1;
    public Transform patrolPoint2;

    // -------------------------------------------------------
    // 缓存引用（Awake 时初始化，供各状态类直接读取，不重复 GetComponent）
    // -------------------------------------------------------
    public Rigidbody2D Rb       { get; private set; }
    public Animator    Anim     { get; private set; }
    public Transform   PlayerTransform { get; private set; }

    public EnemyStateMachine StateMachine { get; private set; }

    private Health _health;

    // -------------------------------------------------------
    // EnemyBaseData 属性代理
    // -------------------------------------------------------
    public float PatrolSpeed    => data.patrolSpeed;
    public float PatrolWaitTime => data.patrolWaitTime;
    public float ChaseSpeed     => data.chaseSpeed;
    public float ChaseRange     => data.chaseRange;
    public float AttackRange    => data.attackRange;
    public float AttackDamage   => data.attackDamage;
    public float AttackRate     => data.attackRate;
    public float HurtDuration   => data.hurtDuration;
    public Transform PatrolPoint1 => patrolPoint1;
    public Transform PatrolPoint2 => patrolPoint2;

    // -------------------------------------------------------
    // Unity 生命周期
    // -------------------------------------------------------

    private void Awake()
    {
        Rb   = GetComponent<Rigidbody2D>();
        Anim = GetComponent<Animator>();
        PlayerTransform = GameObject.FindGameObjectWithTag("Player")?.transform;

        _health = GetComponent<Health>();
        _health.maxHealth     = data.maxHealth;
        _health.currentHealth = data.maxHealth;

        InitializeStateMachine();
    }

    private void InitializeStateMachine()
    {
        StateMachine = new EnemyStateMachine();

        // 预实例化所有状态，切换时不产生 GC
        StateMachine.RegisterState(new PatrolState(this));
        StateMachine.RegisterState(new ChaseState(this));
        StateMachine.RegisterState(new AttackState(this));
        StateMachine.RegisterState(new HurtState(this));
        StateMachine.RegisterState(new DeadState(this));

        StateMachine.Initialize(EnemyStateType.Patrol);
    }

    private void Update()       => StateMachine.Update();
    private void FixedUpdate()  => StateMachine.FixedUpdate();

    // -------------------------------------------------------
    // IDamageable
    // -------------------------------------------------------

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
            // 把受伤前的状态告知 HurtState，恢复时切回正确状态
            StateMachine.GetState<HurtState>()?.SetPreviousState(StateMachine.CurrentStateType);
            StateMachine.TransitionTo(EnemyStateType.Hurt);
        }
    }
}
