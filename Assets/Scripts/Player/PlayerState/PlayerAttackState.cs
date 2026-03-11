using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Attack;

    public PlayerAttackState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        base.Enter();
        anim.SetTrigger("Attack");
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
