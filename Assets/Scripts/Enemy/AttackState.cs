using UnityEngine;

public class AttackState : EnemyState
{
    public override EnemyStateType StateType => EnemyStateType.Attack;

    // 攻击动画执行期为 true，攻击结算后恢复 false。
    private bool _isAttacking;

    public AttackState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
        _isAttacking = false;
        if (anim != null) anim.CrossFade(AttackHash, 0.1f);
    }

    /// <summary>
    /// 攻击状态每帧更新：维持朝向、检测退出距离、满足条件时执行攻击。
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

        // 冷却统一由 EnemyAI 管理，跨状态切换仍保持一致。
        if (enemy.CanAttackNow)
        {
            PerformAttack();
        }
    }

    /// <summary>
    /// 攻击进行中只能被受击和死亡打断；
    /// 非攻击执行期额外允许切回追击状态。
    /// </summary>
    public override bool CanTransitionTo(EnemyStateType targetState)
    {
        if (targetState == EnemyStateType.Hurt || targetState == EnemyStateType.Dead)
        {
            return true;
        }

        if (!_isAttacking && targetState == EnemyStateType.Chase)
        {
            return true;
        }

        return false;
    }

    /// <summary>
    /// 执行伤害结算，并在命中时播放结果音效。
    /// </summary>
    private void PerformAttack()
    {
        _isAttacking = true;

        if (player != null && player.TryGetComponent<IDamageable>(out IDamageable damageable))
        {
            damageable.TakeDamage(enemy.AttackDamage);
            enemy.MarkAttackPerformed();
            // 命中结果音效当前先不触发，后续接入命中音效时再按需恢复。
        }

        _isAttacking = false;
    }
}
