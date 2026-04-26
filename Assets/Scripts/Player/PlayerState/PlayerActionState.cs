using UnityEngine;

/// <summary>
/// 玩家动作状态。
/// 当前用于承接 Slash，一段时间内锁定在动作相位，再回到可移动相位。
/// </summary>
public class PlayerActionState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Action;

    private float _actionTimer;
    private float _actionDuration;
    private PlayerActionKind _currentActionKind;
    private PlayerActionKind _queuedActionKind;

    public PlayerActionKind CurrentActionKind => _currentActionKind;

    public PlayerActionState(PlayerController player) : base(player) { }

    /// <summary>
    /// 消费挂起的动作类型，并根据动作类型设置锁定时长。
    /// </summary>
    public override void Enter()
    {
        _actionTimer = 0f;
        _queuedActionKind = PlayerActionKind.None;
        PlayerActionKind actionKind = player.ConsumePendingActionKind();
        _currentActionKind = actionKind;

        switch (actionKind)
        {
            case PlayerActionKind.Slash:
            case PlayerActionKind.SlashAlt:
                _actionDuration = player.PlayerData.slashDuration;
                break;
            default:
                _actionDuration = 0f;
                break;
        }
    }

    public override void Exit()
    {
        _currentActionKind = PlayerActionKind.None;
        _queuedActionKind = PlayerActionKind.None;
    }

    /// <summary>
    /// 动作锁定结束后回到可移动相位；在地面上时允许根据输入刷新朝向。
    /// </summary>
    public override void Update()
    {
        _actionTimer += Time.deltaTime;
        if (_actionTimer >= _actionDuration)
        {
            if (TryConsumeQueuedCombo())
            {
                return;
            }

            ReturnToLocomotionState();
            return;
        }

        if (player.IsGround)
        {
            FlipCharacter();
        }
    }

    /// <summary>
    /// 动作状态默认不额外覆盖地面/空中移动，只保留平滑速度修正。
    /// </summary>
    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    /// <summary>
    /// Slash 锁定期间只允许受击或死亡打断；时间结束后允许退出到其他相位。
    /// </summary>
    /// <param name="state">目标状态。</param>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        if (state == PlayerStateType.Hurt || state == PlayerStateType.Dead)
        {
            return true;
        }

        // Slash 锁定期间阻止离开 Action；动作时间结束后允许返回可移动相位。
        return _actionTimer >= _actionDuration;
    }

    /// <summary>
    /// 在第一段地面斩击期间缓存第二段输入。
    /// </summary>
    public bool TryQueueGroundCombo(PlayerActionKind actionKind)
    {
        if (actionKind != PlayerActionKind.SlashAlt)
        {
            return false;
        }

        if (_currentActionKind != PlayerActionKind.Slash)
        {
            return false;
        }

        if (_queuedActionKind != PlayerActionKind.None)
        {
            return false;
        }

        if (!player.IsGround)
        {
            return false;
        }

        _queuedActionKind = actionKind;
        return true;
    }

    private bool TryConsumeQueuedCombo()
    {
        if (_queuedActionKind == PlayerActionKind.None || !player.IsGround)
        {
            return false;
        }

        PlayerActionKind nextActionKind = _queuedActionKind;
        _queuedActionKind = PlayerActionKind.None;
        return player.TryStartAction(nextActionKind);
    }
}