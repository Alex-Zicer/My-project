using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerJumpState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Jump;

    public PlayerJumpState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        anim.SetBool("Jump", true);
    }

    public override void Update()
    {
        base.Update();
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Jump;
    }
}
