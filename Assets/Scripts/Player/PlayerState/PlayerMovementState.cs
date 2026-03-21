using UnityEngine;

public class PlayerMovementState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Movement;

    public PlayerMovementState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        anim.CrossFade(MovementHash, 0.1f);
        anim.SetBool(IsGroundHash, true);
        // 落地恢复跳跃次数
        player.StateMachine.GetState<PlayerJumpState>()?.ResetJumps();
    }

    public override void Update()
    {
        float speed = Mathf.Abs(player.MoveInput.x);
        anim.SetFloat(HorizontalSpeedHash, speed);

        if (!player.IsGround)
            player.StateMachine.TransitionTo(PlayerStateType.Fall);

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
