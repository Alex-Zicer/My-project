using UnityEngine;

public class PlayerLandState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Land;

    private float _landTimer;
    private const float LandDuration = 0.06f;

    public PlayerLandState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        _landTimer = 0f;
        // 落地恢复跳跃次数
        player.StateMachine.GetState<PlayerJumpState>()?.ResetJumps();
    }

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

    public override void FixedUpdate()
    {
        // 落地窗口依然维持水平运动，避免因为地面摩擦导致刚进入 Land 就掉成 0 速。
        SmoothSpeed();
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Land;
    }
}
