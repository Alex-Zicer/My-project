using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerRunState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Run;

    public PlayerRunState(PlayerController player) : base(player) { }

    public override void Update()
    {
        if (player.IsGround && Mathf.Abs(player.MoveInput.x) < 0.1f)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Idle);
        }
        if (!player.IsGround)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Fall);
        }
    }

    /// <summary>
    /// 设置玩家进入奔跑状态时，速度更加丝滑，不会出现突然变向
    /// </summary>
    public override void FixedUpdate()
    {
        float targetXVelocity = player.MoveInput.x * player.PlayerData.moveSpeed;
        float currentX = rb.velocity.x;
        float newX = Mathf.MoveTowards(currentX, targetXVelocity, player.PlayerData.moveSpeedMultiplier * Time.fixedDeltaTime);
        rb.velocity = new Vector2(newX, rb.velocity.y);
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        return state != PlayerStateType.Run;
    }
}
