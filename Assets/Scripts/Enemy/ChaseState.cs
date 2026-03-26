using UnityEngine;

public class ChaseState : EnemyState
{
    public override EnemyStateType StateType => EnemyStateType.Chase;

    public ChaseState(EnemyAI enemy) : base(enemy) { }

    /// <summary>
    /// 进入追击状态时播放追击动画。
    /// </summary>
    public override void Enter()
    {
        if (anim != null) anim.CrossFade(ChaseHash, 0.1f);
    }

    /// <summary>
    /// 追击逻辑：
    /// 1. 玩家不存在则回巡逻；
    /// 2. 进入攻击范围就立即切到攻击状态；
    /// 3. 超出追击范围则回巡逻。
    /// </summary>
    public override void Update()
    {
        if (player == null)
        {
            enemy.StateMachine.TransitionTo(EnemyStateType.Patrol);
            return;
        }

        float dist = GetDistanceToPlayer();

        // 攻击冷却由 AttackState/EnemyAI 处理，这里只负责切状态，
        // 避免冷却期间仍然保持追击速度，把玩家持续顶走。
        if (dist <= enemy.AttackRange)
        {
            enemy.StateMachine.TransitionTo(EnemyStateType.Attack);
        }
        else if (dist > enemy.ChaseRange)
        {
            enemy.StateMachine.TransitionTo(EnemyStateType.Patrol);
        }
    }

    /// <summary>
    /// 物理追击：靠近到攻击范围时停止水平速度，避免互相撞飞/推滑。
    /// </summary>
    public override void FixedUpdate()
    {
        if (player == null) return;

        float dist = GetDistanceToPlayer();
        if (dist <= enemy.AttackRange)
        {
            rb.velocity = new Vector2(0f, rb.velocity.y);
            FlipTowardsDirection(GetDirectionToPlayer().x);
            return;
        }

        Vector2 dir = GetDirectionToPlayer();
        rb.velocity = new Vector2(dir.x * enemy.ChaseSpeed, rb.velocity.y);
        FlipTowardsDirection(dir.x);
    }

    /// <summary>
    /// 退出追击时清零水平速度，保留垂直速度。
    /// </summary>
    public override void Exit()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    public override bool CanTransitionTo(EnemyStateType targetState)
        => targetState == EnemyStateType.Patrol ||
           targetState == EnemyStateType.Attack ||
           targetState == EnemyStateType.Hurt ||
           targetState == EnemyStateType.Dead;
}
