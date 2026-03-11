using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Run;

    public PlayerRunState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        anim.SetBool("Run", true);
    }

    public override void Update()
    {

    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Run;
    }
}
