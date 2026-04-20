using UnityEngine;

public class PlayerFallState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Fall;

    public PlayerFallState(PlayerController player) : base(player) { }

    /// <summary>
    /// 进入下落状态，动画过渡由 Knight.controller 的 VerticalSpeed &lt; 0 条件驱动
    /// </summary>
    public override void Enter()
    {
    }

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

    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    /// <summary>
    /// 不可向下落或跳跃状态转换
    /// </summary>
    /// <param name="state">目标状态</param>
    /// <returns></returns>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Fall;
    }
}
