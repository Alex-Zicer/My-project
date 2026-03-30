using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerHurtState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Hurt;

    private float hurtDuration = 0.5f;
    private float hurtTimer;
    public PlayerHurtState(PlayerController player) : base(player) { }

    /// <summary>
    /// 设置触发器，并初始化硬直计时器
    /// </summary>
    public override void Enter()
    {
        hurtTimer = 0;
        if (!IsAnimatorReady()) return;

        // 使用完整路径 hash + 显式 layerIndex，避免状态在不同层/子状态机时找不到
        anim.CrossFade(HurtHash, 0.1f, BaseLayerIndex);
    }

    /// <summary>
    /// 受击0.5秒后切换回移动状态
    /// </summary>
    public override void Update()
    {
        hurtTimer += Time.deltaTime;
        if (hurtTimer > hurtDuration)
        {
            ReturnToMovementState();
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
