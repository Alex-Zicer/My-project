using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Movement;

    public PlayerMovementState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        anim.CrossFade(MovementHash, 0.1f);
        anim.SetBool(IsGroundHash, true);
    }

    public override void Update()
    {
        float speed = Mathf.Abs(player.MoveInput.x);
        anim.SetFloat(HorizontalSpeedHash, speed);

        if (!player.IsGround)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Fall);
        }

        FlipCharacter();
    }

    /// <summary>
    /// 设置平滑转向
    /// </summary>
    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Movement;
    }
}
