using UnityEngine;

public class PlayerActionState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Action;

    private float _actionTimer;
    private float _actionDuration;

    public PlayerActionState(PlayerController player) : base(player) { }

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

    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

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