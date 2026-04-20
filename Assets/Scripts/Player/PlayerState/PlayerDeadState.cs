using UnityEngine;

public class PlayerDeadState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Dead;

    public PlayerDeadState(PlayerController player) : base(player) { }

    /// <summary>
    /// 速度归零并切换为运动学状态，禁用输入。
    /// IsDead=true 已由 PlayerController.TakeDamage 写入 Animator。
    /// </summary>
    public override void Enter()
    {
        rb.velocity = Vector2.zero;
        rb.isKinematic = true; // 脱离物理规律
        player.enabled = false;
    }

    public override void Exit()
    {
        rb.isKinematic = false;
    }

    /// <summary>
    /// 无法向其他状态转换
    /// </summary>
    /// <param name="state">目标状态</param>
    /// <returns></returns>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        return false;
    }
}
