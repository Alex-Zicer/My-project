using UnityEngine;

/// <summary>
/// 玩家冲刺状态。
/// 在短时间内接管速度和重力，形成固定方向的地面 Dash。
/// </summary>
public class PlayerDashState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Dash;

    private float _dashTimer;
    private float _dashDirection;
    private float _cachedGravityScale;

    public PlayerDashState(PlayerController player) : base(player) { }

    /// <summary>
    /// 进入 Dash 时缓存重力、确定冲刺方向，并立即施加冲刺速度。
    /// </summary>
    public override void Enter()
    {
        _dashTimer = 0f;
        _cachedGravityScale = rb.gravityScale;
        _dashDirection = Mathf.Abs(player.MoveInput.x) > 0.1f ? Mathf.Sign(player.MoveInput.x) : player.FacingDirectionX;
        FaceWorldDirection(_dashDirection);

        rb.gravityScale = 0f;
        rb.velocity = new Vector2(_dashDirection * player.PlayerData.dashSpeed, 0f);
    }

    /// <summary>
    /// 退出 Dash 时恢复重力。
    /// </summary>
    public override void Exit()
    {
        rb.gravityScale = _cachedGravityScale;
    }

    /// <summary>
    /// Dash 持续到时间结束后回到可移动相位。
    /// </summary>
    public override void Update()
    {
        _dashTimer += Time.deltaTime;
        if (_dashTimer >= player.PlayerData.dashDuration)
        {
            ReturnToLocomotionState();
        }
    }

    /// <summary>
    /// Dash 期间持续维持固定水平速度。
    /// </summary>
    public override void FixedUpdate()
    {
        rb.velocity = new Vector2(_dashDirection * player.PlayerData.dashSpeed, 0f);
    }

    /// <summary>
    /// Dash 进行中只允许被受击或死亡打断；Dash 结束后放行所有状态。
    /// </summary>
    /// <param name="state">目标状态。</param>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        if (state == PlayerStateType.Hurt || state == PlayerStateType.Dead)
            return true;

        // Dash 计时结束后才允许切换到其他状态（由 Update 的 ReturnToLocomotionState 触发）。
        return _dashTimer >= player.PlayerData.dashDuration;
    }
}