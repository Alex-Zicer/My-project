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
        anim.SetFloat("HorizontalSpeed", 0);
    }

    /// <summary>
    /// 检测玩家的输入，是否应该转换到Run
    /// </summary>
    public override void Update()
    {

    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Idle;
    }
}
