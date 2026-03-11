using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public enum PlayerStateType
{
    Idle,
    Run,
    Jump,
    Fall,
    Attack,
    Hurt,
    Dead
}

public interface IPlayerState
{
    /// <summary>
    /// 获取状态的类型
    /// </summary>
    PlayerStateType StateType { get; }

    /// <summary>
    /// 进入状态时调用
    /// </summary>
    void Enter();

    /// <summary>
    /// 退出时调用
    /// </summary>
    void Exit();

    /// <summary>
    /// 每帧更新逻辑
    /// </summary>
    void Update();

    /// <summary>
    /// 物理更新逻辑
    /// </summary>
    void FixedUpdate();

    /// <summary>
    /// 判断是否能够转换到目标状态
    /// </summary>
    /// <param name="state">目标状态</param>
    bool CanTransitionTo(PlayerStateType state);
}

