using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerStateBase : IPlayerState
{
    protected PlayerController player;
    protected Animator anim;
    protected Rigidbody2D rb;


    public abstract PlayerStateType StateType { get; }

    /// <summary>
    /// 构造函数，每个继承这个基类的子类都实现这个构造
    /// </summary>
    /// <param name="player">玩家对象</param>
    public PlayerStateBase(PlayerController player)
    {
        this.player = player;
        this.anim = player.GetComponent<Animator>();
        this.rb = player.GetComponent<Rigidbody2D>();
    }

    /// <summary>
    /// 虚函数，方便子类重写，进入状态时执行的逻辑
    /// </summary>
    public virtual void Enter()
    {

    }

    /// <summary>
    /// 退出状态时执行的逻辑
    /// </summary>
    public virtual void Exit()
    {

    }

    /// <summary>
    /// 每帧更新逻辑
    /// </summary>
    public virtual void Update()
    {

    }

    /// <summary>
    /// 固定时间更新逻辑
    /// </summary>
    public virtual void FixedUpdate()
    {

    }

    /// <summary>
    /// 检测目标状态是否能够转换
    /// </summary>
    /// <param name="state">目标状态</param>
    /// <returns></returns>
    public virtual bool CanTransitionTo(PlayerStateType state)
    {
        return true;
    }

    /// <summary>
    /// 某些状态结束后回到移动模式
    /// </summary>
    protected void ReturnToMovementState()
    {
        if (Mathf.Abs(player.MoveInput.x) > 0.1f && player.IsGround)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Run);
        }
        else if (player.IsGround)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Idle);
        }
        else
        {
            player.StateMachine.TransitionTo(PlayerStateType.Fall);
        }
    }

    /// <summary>
    /// 改变人物朝向，基于玩家的输入来判断人物应该面朝左还是面朝右
    /// </summary>
    protected void FlipCharacter()
    {
        float direction = player.MoveInput.x;

        if (direction > 0.1f)
        {
            player.transform.localScale = new Vector3(1, 1, 1); //面朝右
        }
        else if (direction < -0.1f)
        {
            player.transform.localScale = new Vector3(-1, 1, 1); //面朝左
        }
    }
}
