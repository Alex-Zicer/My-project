using UnityEngine;

/// <summary>
/// 玩家下落状态。
/// 负责处理自然下落、落地，以及贴墙下滑的分流。
/// </summary>
public class PlayerFallState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Fall;

    public PlayerFallState(PlayerController player) : base(player) { }

    /// <summary>
    /// 进入下落状态。
    /// 动画切换由 Animator 中的 VerticalSpeed 和 IsGround 条件驱动，这里不直接写动画参数。
    /// </summary>
    public override void Enter()
    {
    }

    /// <summary>
    /// 下落过程中检测落地或进入 WallSlide 的条件。
    /// </summary>
    public override void Update()
    {
        if (player.IsGround && rb.velocity.y <= 0.1f)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Land);
            return;
        }

        if (player.CanWallSlide)
        {
            player.StateMachine.TransitionTo(PlayerStateType.WallSlide);
            return;
        }

        FlipCharacter();
    }

    /// <summary>
    /// 下落状态仍允许空中横向修正。
    /// </summary>
    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    /// <summary>
    /// Fall 状态不允许切换到自身。
    /// </summary>
    /// <param name="state">目标状态。</param>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Fall;
    }
}
