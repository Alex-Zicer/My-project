using UnityEngine;

public class PlayerWallSlideState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.WallSlide;

    public PlayerWallSlideState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -player.PlayerData.wallSlideSpeed));
    }

    public override void Update()
    {
        if (player.IsGround)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Land);
            return;
        }

        if (!player.CanWallSlide)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Fall);
        }
    }

    public override void FixedUpdate()
    {
        float targetXVelocity = player.MoveInput.x * player.PlayerData.moveSpeed;
        float currentX = rb.velocity.x;
        float newX = Mathf.MoveTowards(currentX, targetXVelocity, player.PlayerData.moveSpeedMultiplier * Time.fixedDeltaTime);
        float clampedY = Mathf.Max(rb.velocity.y, -player.PlayerData.wallSlideSpeed);
        rb.velocity = new Vector2(newX, clampedY);
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.WallSlide;
    }
}