using UnityEngine;

/// <summary>
/// 玩家落地状态。
/// 用一个很短的落地窗口承接 Fall 到 Movement 的过渡，并在窗口中保持水平控制。
/// </summary>
public class PlayerLandState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Land;

    private float _landTimer;
    private const float LandDuration = 0.06f;

    public PlayerLandState(PlayerController player) : base(player) { }

    /// <summary>
    /// 进入落地状态时清零计时器，并恢复跳跃次数。
    /// </summary>
    public override void Enter()
    {
        _landTimer = 0f;
        // 落地恢复跳跃次数
        player.StateMachine.GetState<PlayerJumpState>()?.ResetJumps();
    }

    /// <summary>
    /// 处理落地窗口：离地时回 Fall，计时结束后回到可移动相位。
    /// </summary>
    public override void Update()
    {
        if (!player.IsGround)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Fall);
            return;
        }

        _landTimer += Time.deltaTime;
        if (_landTimer >= LandDuration)
        {
            ReturnToLocomotionState();
        }
    }

    /// <summary>
    /// 落地窗口内继续维持水平运动，避免地面摩擦导致一落地就掉速。
    /// </summary>
    public override void FixedUpdate()
    {
        // 落地窗口依然维持水平运动，避免因为地面摩擦导致刚进入 Land 就掉成 0 速。
        SmoothSpeed();
    }

    /// <summary>
    /// Land 状态不允许切换到自身。
    /// </summary>
    /// <param name="state">目标状态。</param>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Land;
    }
}
