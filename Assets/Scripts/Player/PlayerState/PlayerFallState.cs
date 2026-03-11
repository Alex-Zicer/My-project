using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerFallState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Fall;

    public PlayerFallState(PlayerController player) : base(player) { }

    public override void Update()
    {
        if (player.IsGround)
        {
            ReturnToMovementState();
        }
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Fall;
    }
}
