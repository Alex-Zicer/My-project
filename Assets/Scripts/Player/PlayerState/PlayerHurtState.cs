using UnityEngine;

public class PlayerHurtState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Hurt;

    private float hurtDuration = 0.5f;
    private float hurtTimer;
    public PlayerHurtState(PlayerController player) : base(player) { }

    /// <summary>
    /// 初始化硬直计时器（Hurt Trigger 已由 PlayerController.TakeDamage 触发）
    /// </summary>
    public override void Enter()
    {
        hurtTimer = 0;
    }

    /// <summary>
    /// 受击0.5秒后切换回移动状态
    /// </summary>
    public override void Update()
    {
        hurtTimer += Time.deltaTime;
        if (hurtTimer > hurtDuration)
        {
            ReturnToLocomotionState();
        }
    }

    /// <summary>
    /// 检测能否转换到对应状态
    /// </summary>
    /// <param name="state">目标状态</param>
    /// <returns></returns>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        //受击状态结束之后可以向任何状态转变
        if (hurtTimer >= hurtDuration)
        {
            return true;
        }
        //受击过程中只能转换到死亡状态
        return state == PlayerStateType.Dead;
    }
}
