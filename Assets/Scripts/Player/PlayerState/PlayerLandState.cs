using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerLandState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Land;

    public PlayerLandState(PlayerController player) : base(player) { }

    private float landTimer;
    private float landDuration = 0.33f;
    public override void Enter()
    {
        anim.CrossFade(LandHash, 0.05f);
        anim.SetBool(IsGroundHash, true);
        anim.SetFloat(VerticalSpeedHash, 0f);
    }

    public override void Update()
    {
        landTimer += Time.deltaTime;

        if (landTimer > landDuration) ReturnToMovementState();
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Land;
    }
}
