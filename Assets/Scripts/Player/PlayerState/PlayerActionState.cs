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

    public PlayerActionState(PlayerController player) : base(player) { }

    /// <summary>
    /// 消费挂起的动作类型，并根据动作类型设置锁定时长。
    /// </summary>
    public override void Enter()
    {
        _actionTimer = 0f;
        PlayerActionKind actionKind = player.ConsumePendingActionKind();

        switch (actionKind)
        {
            case PlayerActionKind.Slash:
                _actionDuration = player.PlayerData.slashDuration;
                break;
            default:
                _actionDuration = 0f;
                break;
        }
    }

    /// <summary>
    /// 动作锁定结束后回到可移动相位；在地面上时允许根据输入刷新朝向。
    /// </summary>
    public override void Update()
    {
        _actionTimer += Time.deltaTime;
        if (_actionTimer >= _actionDuration)
        {
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
}