using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerIdleState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Idle;

    public PlayerIdleState(PlayerController player) : base(player) { }

    /// <summary>
    /// 设置动画机的参数，是人物进入对应动画
    /// </summary>
    public override void Enter()
    {
        anim.SetBool("IsGround", true);
    }

    /// <summary>
    /// 检测玩家的输入，是否应该转换到Run或者Fall
    /// </summary>
    public override void Update()
    {
        if (Mathf.Abs(player.MoveInput.x) > 0.1f)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Run);
            return;
        }

        if (!player.IsGround)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Fall);
        }
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Idle;
    }
}
