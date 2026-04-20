using UnityEngine;

public abstract class PlayerStateBase : IPlayerState
{
    protected PlayerController player;
    protected Rigidbody2D rb;

    public abstract PlayerStateType StateType { get; }

    /// <summary>
    /// 构造函数，每个继承这个基类的子类都实现这个构造
    /// </summary>
    /// <param name="player">玩家对象</param>
    public PlayerStateBase(PlayerController player)
    {
        this.player = player;
        this.rb = player.Rb;
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
    /// 某些状态结束后回到可移动相位
    /// </summary>
    protected void ReturnToLocomotionState()
    {
        if (player.IsGround)
        {
            player.StateMachine.TransitionTo(PlayerStateType.Movement);
        }
        else if (player.CanWallSlide)
        {
            player.StateMachine.TransitionTo(PlayerStateType.WallSlide);
        }
        else
        {
            player.StateMachine.TransitionTo(PlayerStateType.Fall);
        }
    }

    /// <summary>
    /// 改变人物朝向，基于玩家的输入来判断人物应该面朗左还是面朗右。
    /// Knight 精灵默认朝左：scale.x = 1 就是左，scale.x = -1 就是右。
    /// </summary>
    protected void FlipCharacter()
    {
        float direction = player.MoveInput.x;

        if (direction > 0.1f)
        {
            player.transform.localScale = new Vector3(-1, 1, 1); // 面朝右（翻转）
        }
        else if (direction < -0.1f)
        {
            player.transform.localScale = new Vector3(1, 1, 1);  // 面朝左（默认）
        }
    }

    /// <summary>
    /// 直接按世界方向设置朝向。direction &gt; 0 表示朝右，&lt; 0 表示朝左。
    /// </summary>
    protected void FaceWorldDirection(float direction)
    {
        if (direction > 0.1f)
        {
            player.transform.localScale = new Vector3(-1, 1, 1);
        }
        else if (direction < -0.1f)
        {
            player.transform.localScale = new Vector3(1, 1, 1);
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
