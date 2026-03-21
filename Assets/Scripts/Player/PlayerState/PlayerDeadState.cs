using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeadState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Dead;

    public PlayerDeadState(PlayerController player) : base(player) { }

    /// <summary>
    /// 设置死亡动画触发器，把玩家速度调整为0，并禁用玩家的输入
    /// </summary>
    public override void Enter()
    {
        anim.CrossFade(DeadHash, 0.1f);
        rb.velocity = Vector2.zero;
        rb.isKinematic = true;//脱离物理规律

        player.enabled = false;
    }

    public override void Exit()
    {
        rb.isKinematic = true;
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
