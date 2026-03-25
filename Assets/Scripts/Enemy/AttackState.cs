using UnityEngine;

public class AttackState : EnemyState
{
    public override EnemyStateType StateType => EnemyStateType.Attack;

    private bool _isAttacking; // 攻击动画播放期间为 true，冷却阶段为 false

    public AttackState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
        _isAttacking = false;
        if (anim != null) anim.CrossFade(AttackHash, 0.1f);
    }

    /// <summary>
    /// 玩家死亡切换回巡逻模式
    /// 时刻保持精灵的正确朝向
    /// 玩家脱离攻击范围切换回追击状态
    /// </summary>
    public override void Update()
    {
        if (player == null)
        {
            enemy.StateMachine.TransitionTo(EnemyStateType.Patrol);
            return;
        }

        FlipTowardsDirection(GetDirectionToPlayer().x);

        if (GetDistanceToPlayer() > enemy.AttackExitRange)
        {
            enemy.StateMachine.TransitionTo(EnemyStateType.Chase);
            return;
        }

        // 冷却统一由 EnemyAI 管理，跨状态切换仍保留
        if (enemy.CanAttackNow)
        {
            PerformAttack();
        }
    }

    /// <summary>
    /// 攻击进行中只能被受击和死亡打断；
    /// 冷却阶段（_isAttacking == false）额外允许切换到追击状态。
    /// </summary>
    public override bool CanTransitionTo(EnemyStateType targetState)
    {
        if (targetState == EnemyStateType.Hurt || targetState == EnemyStateType.Dead)
            return true;
        if (!_isAttacking && targetState == EnemyStateType.Chase)
            return true;
        return false;
    }

    /// <summary>
    /// 执行伤害逻辑计算
    /// </summary>
    private void PerformAttack()
    {
        _isAttacking = true;

        if (player != null && player.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            damageable.TakeDamage(enemy.AttackDamage);
            enemy.MarkAttackPerformed();
        }

        _isAttacking = false;
    }
}
