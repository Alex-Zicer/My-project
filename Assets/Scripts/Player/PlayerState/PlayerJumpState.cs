using UnityEngine;

/// <summary>
/// 玩家跳跃状态。
/// 负责普通跳、二段跳和墙跳的起跳施力，并在空中根据地面/墙体情况切换到后续相位。
/// </summary>
public class PlayerJumpState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Jump;

    private const int MaxJumpCount = 2;
    private int _remainingJumps;
    private PlayerJumpKind _currentJumpKind;
    // 起跳后的最短腾空时间，防止起跳帧 IsGround 延迟导致立即落地判定
    private float _airtimeTimer;
    private const float MinAirtime = 0.1f;

    /// <summary>
    /// 当前剩余跳跃次数。
    /// 主要用于调试日志和外部观测，不直接暴露修改入口。
    /// </summary>
    public int RemainingJumps => _remainingJumps;

    public PlayerJumpState(PlayerController player) : base(player) { }

    /// <summary>
    /// 外部调用，判断并消耗一次普通跳跃机会。
    /// 地面起跳永远视为 Normal；空中消耗最后一次机会时视为 Double。
    /// </summary>
    public bool TryConsumeStandardJump(bool isGrounded, out PlayerJumpKind jumpKind)
    {
        jumpKind = PlayerJumpKind.None;

        if (isGrounded)
        {
            _remainingJumps = MaxJumpCount - 1;
            jumpKind = PlayerJumpKind.Normal;
            return true;
        }

        if (_remainingJumps <= 0)
        {
            return false;
        }

        _remainingJumps--;
        jumpKind = _remainingJumps == 0 ? PlayerJumpKind.Double : PlayerJumpKind.Normal;
        return true;
    }

    /// <summary>
    /// 外部调用，消耗一次墙跳机会，并在起跳后保留一次空中二段跳。
    /// </summary>
    public bool TryConsumeWallJump(out PlayerJumpKind jumpKind)
    {
        _remainingJumps = MaxJumpCount - 1;
        jumpKind = PlayerJumpKind.Wall;
        return true;
    }

    /// <summary>
    /// 落地时调用，恢复全部跳跃次数。
    /// </summary>
    public void ResetJumps()
    {
        _remainingJumps = MaxJumpCount;
    }

    /// <summary>
    /// 根据挂起的 JumpKind 一次性施加起跳速度。
    /// 墙跳会额外修正朝向与水平速度。
    /// </summary>
    public override void Enter()
    {
        _currentJumpKind = player.ConsumePendingJumpKind();
        if (_currentJumpKind == PlayerJumpKind.None)
        {
            _currentJumpKind = PlayerJumpKind.Normal;
        }

        _airtimeTimer = 0f;

        if (_currentJumpKind == PlayerJumpKind.Double && player.EffectSpawner != null)
        {
            player.EffectSpawner.PlayDoubleJumpEffect();
        }

        if (_currentJumpKind == PlayerJumpKind.Wall)
        {
            float wallJumpDirection = -player.FacingDirectionX;
            FaceWorldDirection(wallJumpDirection);
            rb.velocity = new Vector2(wallJumpDirection * player.PlayerData.wallJumpHorizontalSpeed,
                                      player.PlayerData.wallJumpForce);
        }
        else
        {
            rb.velocity = new Vector2(rb.velocity.x, player.PlayerData.JumpForce);
        }
    }

    /// <summary>
    /// 在空中检测落地、贴墙和自然下落的相位切换。
    /// </summary>
    public override void Update()
    {
        _airtimeTimer += Time.deltaTime;
        // MinAirtime 内不做落地判断，避免起跳帧 IsGround 还为 true 导致立即结束
        if (_airtimeTimer < MinAirtime) { FlipCharacter(); return; }

        if (player.IsGround)
        {
            // 落地时 velocity.y 被物理引擎归零，不一定会走到 y<0，必须单独判断
            player.StateMachine.TransitionTo(PlayerStateType.Land);
        }
        else if (player.CanWallSlide)
        {
            player.StateMachine.TransitionTo(PlayerStateType.WallSlide);
        }
        else if (rb.velocity.y < 0)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Fall);
        }

        FlipCharacter();
    }

    /// <summary>
    /// 空中状态仍允许水平移动修正。
    /// </summary>
    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    /// <summary>
    /// Jump 状态的合法切换范围。
    /// 允许切到 Fall、Land、WallSlide，以及再次进入 Jump 以承接二段跳。
    /// </summary>
    /// <param name="state">目标状态。</param>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state == PlayerStateType.Dead ||
               state == PlayerStateType.Action ||
               state == PlayerStateType.Hurt ||
               state == PlayerStateType.Fall ||
               state == PlayerStateType.Land ||
               state == PlayerStateType.Dash ||
               state == PlayerStateType.WallSlide ||
               state == PlayerStateType.Jump;   // 二段跳
    }
}
