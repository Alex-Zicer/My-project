using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerDeadState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Dead;

    public PlayerDeadState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
    }

    public override void Exit()
    {
        base.Exit();
    }

    public override void Update()
    {
        base.Update();
    }

    public override void FixedUpdate()
    {
        base.FixedUpdate();
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return base.CanTransitionTo(state);
    }
}
