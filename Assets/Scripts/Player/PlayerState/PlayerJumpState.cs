using UnityEngine;

public class PlayerJumpState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Jump;

    private int _remainingJumps;

    public PlayerJumpState(PlayerController player) : base(player) { }

    /// <summary>
    /// 外部（PlayerController.OnJumpPerformed）调用，判断并消耗一次跳跃机会。
    /// 落地时通过 ResetJumps 重置。
    /// </summary>
    public bool TryConsumeJump()
    {
        if (_remainingJumps <= 0) return false;
        _remainingJumps--;
        return true;
    }

    /// <summary>
    /// 落地时（MovementState.Enter / LandState.Enter）调用，恢复全部跳跃次数。
    /// </summary>
    public void ResetJumps()
    {
        _remainingJumps = player.canJumpCount;
    }

    public override void Enter()
    {
        if (IsAnimatorReady())
        {
            anim.CrossFade(JumpHash, 0.1f);
            anim.SetBool(IsGroundHash, false);
        }
        rb.velocity = new Vector2(rb.velocity.x, player.PlayerData.JumpForce);
    }

    public override void Update()
    {
        if (IsAnimatorReady())
            anim.SetFloat(VerticalSpeedHash, rb.velocity.y);

        if (rb.velocity.y < 0)
            player.StateMachine.TransitionTo(PlayerStateType.Fall);

        FlipCharacter();
    }

    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state == PlayerStateType.Dead  ||
               state == PlayerStateType.Hurt  ||
               state == PlayerStateType.Fall  ||
               state == PlayerStateType.Jump;   // 二段跳
    }
}
