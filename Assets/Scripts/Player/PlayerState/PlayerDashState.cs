using UnityEngine;

public class PlayerDashState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Dash;

    private float _dashTimer;
    private float _dashDirection;
    private float _cachedGravityScale;

    public PlayerDashState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        _dashTimer = 0f;
        _cachedGravityScale = rb.gravityScale;
        _dashDirection = Mathf.Abs(player.MoveInput.x) > 0.1f ? Mathf.Sign(player.MoveInput.x) : player.FacingDirectionX;
        FaceWorldDirection(_dashDirection);

        rb.gravityScale = 0f;
        rb.velocity = new Vector2(_dashDirection * player.PlayerData.dashSpeed, 0f);
    }

    public override void Exit()
    {
        rb.gravityScale = _cachedGravityScale;
    }

    public override void Update()
    {
        _dashTimer += Time.deltaTime;
        if (_dashTimer >= player.PlayerData.dashDuration)
        {
            ReturnToLocomotionState();
        }
    }

    public override void FixedUpdate()
    {
        rb.velocity = new Vector2(_dashDirection * player.PlayerData.dashSpeed, 0f);
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state == PlayerStateType.Hurt ||
               state == PlayerStateType.Dead;
    }
}