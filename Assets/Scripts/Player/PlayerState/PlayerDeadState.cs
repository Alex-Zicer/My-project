using UnityEngine;

/// <summary>
/// 玩家死亡状态。
/// 进入后停止速度、切为运动学刚体，并禁用控制器输入。
/// </summary>
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

    /// <summary>
    /// 退出死亡状态时恢复刚体的物理模拟配置。
    /// 正常游戏流程中一般不会从 Dead 状态退出，这里仅作保护性恢复。
    /// </summary>
    public override void Exit()
    {
        rb.isKinematic = false;
    }

    /// <summary>
    /// Dead 为终止状态，不允许再切换到其他状态。
    /// </summary>
    /// <param name="state">目标状态。</param>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        return false;
    }
}
