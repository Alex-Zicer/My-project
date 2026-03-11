using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class PlayerStateBase : IPlayerState
{
    protected PlayerController Player;
    protected Animator anim;

    public abstract PlayerStateType StateType { get; }

    /// <summary>
    /// 构造函数，每个继承这个基类的子类都实现这个构造
    /// </summary>
    /// <param name="player">玩家对象</param>
    public PlayerStateBase(PlayerController player)
    {
        this.Player = player;
        anim = player.GetComponent<Animator>();
    }

    /// <summary>
    /// 抽象函数，方便子类重写
    /// </summary>
    public virtual void Enter()
    {
        
    }

    public virtual void Exit()
    {
        
    }

    public virtual void Update()
    {
        
    }

    public virtual void FixedUpdate()
    {
        
    }

    public virtual bool CanTransitionTo(PlayerStateType state)
    {
        return true;
    }
}
