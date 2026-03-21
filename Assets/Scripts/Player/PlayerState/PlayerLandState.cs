using UnityEngine;

public class PlayerLandState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Land;

    private float _landTimer;
    private const float LandDuration = 0.33f;

    public PlayerLandState(PlayerController player) : base(player) { }

    public override void Enter()
    {
        _landTimer = 0f;
        anim.CrossFade(LandHash, 0.05f);
        anim.SetBool(IsGroundHash, true);
        anim.SetFloat(VerticalSpeedHash, 0f);
        // 落地恢复跳跃次数
        player.StateMachine.GetState<PlayerJumpState>()?.ResetJumps();
    }

    public override void Update()
    {
        _landTimer += Time.deltaTime;
        if (_landTimer > LandDuration) ReturnToMovementState();
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Land;
    }
}
