using UnityEngine;

public class ChaseState : EnemyState
{
    public override EnemyStateType StateType => EnemyStateType.Chase;

    public ChaseState(EnemyAI enemy) : base(enemy) { }

    /// <summary>
    /// 以丝滑的方式播放追击动画
    /// </summary>
    public override void Enter()
    {
        if (anim != null) anim.CrossFade(ChaseHash, 0.1f);
    }

    /// <summary>
    /// 如果玩家不存在，切换到巡逻状态
    /// 如果玩家进入攻击范围切换到攻击状态
    /// 当玩家逃出感知范围切换回巡逻状态
    /// </summary>
    public override void Update()
    {
        if (player == null)
        {
            enemy.StateMachine.TransitionTo(EnemyStateType.Patrol);
            return;
        }

        float dist = GetDistanceToPlayer();

        //根据范围切换攻击状态还是巡逻状态
        if (dist <= enemy.AttackRange)
        {
            if (enemy.CanAttackNow)
            {
                enemy.StateMachine.TransitionTo(EnemyStateType.Attack);
            }
        }
        else if (dist > enemy.ChaseRange)
            enemy.StateMachine.TransitionTo(EnemyStateType.Patrol);
    }

    /// <summary>
    /// 以固定时间内获取与玩家之间的距离
    /// 确保精灵朝向与追击方向一致
    /// </summary>
    public override void FixedUpdate()
    {
        if (player == null) return;
        Vector2 dir = GetDirectionToPlayer();
        rb.velocity = new Vector2(dir.x * enemy.ChaseSpeed, rb.velocity.y);
        FlipTowardsDirection(dir.x);
    }

    /// <summary>
    /// 退出追击状态时将水平速度置为0，以免进入攻击状态时还有水平速度。
    /// </summary>
    public override void Exit()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    /// <summary>
    /// 追击状态能切换到巡逻、攻击、受击和死亡状态
    /// </summary>
    /// <param name="targetState">目标状态</param>
    /// <returns></returns>
    public override bool CanTransitionTo(EnemyStateType targetState)
        => targetState == EnemyStateType.Patrol ||
           targetState == EnemyStateType.Attack ||
           targetState == EnemyStateType.Hurt ||
           targetState == EnemyStateType.Dead;
}
