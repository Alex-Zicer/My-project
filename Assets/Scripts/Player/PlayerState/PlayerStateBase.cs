using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerStateBase : IPlayerState
{
    protected PlayerController player;
    protected Animator anim;
    protected Rigidbody2D rb;

    //动画状态哈希
    protected static readonly int MovementHash = Animator.StringToHash("Movement");
    protected static readonly int JumpHash = Animator.StringToHash("Jump");
    protected static readonly int FallHash = Animator.StringToHash("Fall");
    protected static readonly int LandHash = Animator.StringToHash("Land");
    protected static readonly int HurtHash = Animator.StringToHash("Hurt");
    protected static readonly int DeadHash = Animator.StringToHash("Dead");
    protected static readonly int Attack1Hash = Animator.StringToHash("Attack1");
    protected static readonly int Attack2Hash = Animator.StringToHash("Attack2");

    //动画参数哈希
    protected static readonly int HorizontalSpeedHash = Animator.StringToHash("HorizontalSpeed");
    protected static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    protected static readonly int IsGroundHash = Animator.StringToHash("IsGround");

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
        if (player.IsGround)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Movement);
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

    protected void SmoothSpeed()
    {
        float targetXVelocity = player.MoveInput.x * player.PlayerData.moveSpeed;
        float currentX = rb.velocity.x;
        float newX = Mathf.MoveTowards(currentX, targetXVelocity, player.PlayerData.moveSpeedMultiplier * Time.fixedDeltaTime);
        rb.velocity = new Vector2(newX, rb.velocity.y);
    }
}
