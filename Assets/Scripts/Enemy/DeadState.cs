using UnityEngine;

public class DeadState : EnemyState
{
    public override EnemyStateType StateType => EnemyStateType.Dead;

    public DeadState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        rb.velocity = Vector2.zero;
        if (anim != null) anim.CrossFade(DeadHash, 0.1f);
        // 关闭碰撞，防止死后仍能被攻击或阻挡角色
        var col = enemy.GetComponent<Collider2D>();
        if (col != null) col.enabled = false;
    }

    // 死亡状态不允许任何转换
    public override bool CanTransitionTo(EnemyStateType targetState) => false;
}
