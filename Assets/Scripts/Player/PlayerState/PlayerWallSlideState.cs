using UnityEngine;

/// <summary>
/// 玩家贴墙下滑状态。
/// 负责限制下滑速度，并在离墙或落地时切换到后续相位。
/// </summary>
public class PlayerWallSlideState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.WallSlide;

    public PlayerWallSlideState(PlayerController player) : base(player) { }

    /// <summary>
    /// 进入贴墙状态时先把竖直速度夹到墙滑上限范围内。
    /// </summary>
    public override void Enter()
    {
        rb.velocity = new Vector2(rb.velocity.x, Mathf.Max(rb.velocity.y, -player.PlayerData.wallSlideSpeed));
    }

    /// <summary>
    /// 落地后进入 Land；若不再满足贴墙条件则回到 Fall。
    /// </summary>
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

    /// <summary>
    /// 贴墙下滑状态允许缓慢横向修正，同时限制最大下落速度。
    /// </summary>
    public override void FixedUpdate()
    {
        float targetXVelocity = player.MoveInput.x * player.PlayerData.moveSpeed;
        float currentX = rb.velocity.x;
        float newX = Mathf.MoveTowards(currentX, targetXVelocity, player.PlayerData.moveSpeedMultiplier * Time.fixedDeltaTime);
        float clampedY = Mathf.Max(rb.velocity.y, -player.PlayerData.wallSlideSpeed);
        rb.velocity = new Vector2(newX, clampedY);
    }

    /// <summary>
    /// WallSlide 状态不允许切回自身。
    /// </summary>
    /// <param name="state">目标状态。</param>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.WallSlide;
    }
}