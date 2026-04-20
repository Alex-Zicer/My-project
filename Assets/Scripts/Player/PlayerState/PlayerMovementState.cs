using UnityEngine;

public class PlayerMovementState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Movement;

    public PlayerMovementState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        // 落地恢复跳跃次数
        player.StateMachine.GetState<PlayerJumpState>()?.ResetJumps();
    }

    public override void Update()
    {
        if (!player.IsGround)
        {
            player.StateMachine.TransitionTo(player.CanWallSlide ? PlayerStateType.WallSlide : PlayerStateType.Fall);
            return;
        }

        FlipCharacter();
    }

    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Movement;
    }
}
