using UnityEngine;

/// <summary>
/// 玩家地面移动状态。
/// 负责地面上的移动控制、朝向更新，以及离地后的相位切换。
/// </summary>
public class PlayerMovementState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Movement;

    public PlayerMovementState(PlayerController player) : base(player) { }

    /// <summary>
    /// 进入地面移动状态时恢复全部跳跃次数。
    /// </summary>
    public override void Enter()
    {
        // 落地恢复跳跃次数
        player.StateMachine.GetState<PlayerJumpState>()?.ResetJumps();
    }

    /// <summary>
    /// 地面状态下持续监听是否离地，并根据墙体条件切到 Fall 或 WallSlide。
    /// </summary>
    public override void Update()
    {
        if (!player.IsGround)
        {
            player.StateMachine.GetState<PlayerJumpState>()?.ConsumeLedgeFallJump();
            player.StateMachine.TransitionTo(player.CanWallSlide ? PlayerStateType.WallSlide : PlayerStateType.Fall);
            return;
        }

        FlipCharacter();
    }

    /// <summary>
    /// 在地面状态下平滑更新水平移动速度。
    /// </summary>
    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    /// <summary>
    /// Movement 状态不允许切换到自身，其他目标状态由外部逻辑决定。
    /// </summary>
    /// <param name="state">目标状态。</param>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Movement;
    }
}
