using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

public class PlayerFallState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Fall;

    public PlayerFallState(PlayerController player) : base(player) { }

    /// <summary>
    /// 播放下落动画，设置IsGround参数为false
    /// </summary>
    public override void Enter()
    {
        anim.CrossFade(FallHash, 0.1f);
        anim.SetBool(IsGroundHash, false);
    }

    public override void Update()
    {
        anim.SetFloat(VerticalSpeedHash, rb.velocity.y);

        if (player.IsGround && rb.velocity.y <= 0.1f)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Land);
        }

        FlipCharacter();
    }

    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    /// <summary>
    /// 不可向下落或跳跃状态转换
    /// </summary>
    /// <param name="state">目标状态</param>
    /// <returns></returns>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Fall;
    }
}
