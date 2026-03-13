using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Jump;

    public PlayerJumpState(PlayerController player) : base(player) { }

    /// <summary>
    /// 播放跳跃动画，给一个向上的速度
    /// </summary>
    public override void Enter()
    {
        anim.CrossFade(JumpHash, 0.1f);
        anim.SetBool(IsGroundHash, false);
        rb.velocity = new Vector2(rb.velocity.x, player.PlayerData.JumpForce);
    }

    /// <summary>
    /// 当垂直速度小于0的时候转换为下落状态
    /// </summary>
    public override void Update()
    {
        //每帧更新动画控制器的VerticalSpeed
        anim.SetFloat(VerticalSpeedHash, rb.velocity.y);

        if (rb.velocity.y < 0)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Fall);
        }

        FlipCharacter();
    }

    /// <summary>
    /// 设置速度的转变更为丝滑
    /// </summary>
    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    /// <summary>
    /// 可向受击、死亡和下落状态转换
    /// </summary>
    /// <param name="state">目标状态</param>
    /// <returns></returns>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state == PlayerStateType.Dead || state == PlayerStateType.Hurt || state == PlayerStateType.Fall;
    }
}
