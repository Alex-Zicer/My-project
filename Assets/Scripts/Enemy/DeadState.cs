using UnityEngine;

public class DeadState : EnemyState
{
    public override EnemyStateType StateType => EnemyStateType.Dead;

    public DeadState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        rb.velocity = Vector2.zero;
        if (anim != null) anim.CrossFade(DeadHash, 0.1f);
        enemy.BeginDeathSequence();
    }

    // 死亡状态不允许任何转换
    public override bool CanTransitionTo(EnemyStateType targetState) => false;
}
