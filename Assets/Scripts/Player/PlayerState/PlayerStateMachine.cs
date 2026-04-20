using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 玩家状态机。
/// 负责注册状态、切换状态，并把 Update / FixedUpdate 分发给当前状态实例。
/// </summary>
public class PlayerStateMachine
{
    private Dictionary<PlayerStateType, IPlayerState> states;
    private IPlayerState currentState;
    private PlayerController player;

    public IPlayerState CurrentState => currentState;

    /// <summary>
    /// 获取玩家当前的状态；若当前状态为空，则默认视为 Movement。
    /// </summary>
    public PlayerStateType CurrentStateType => currentState?.StateType ?? PlayerStateType.Movement;

    /// <summary>
    /// 构造函数
    /// </summary>
    /// <param name="player">玩家对象</param>
    public PlayerStateMachine(PlayerController player)
    {
        this.player = player;
        states = new Dictionary<PlayerStateType, IPlayerState>();
    }

    /// <summary>
    /// 注册状态到字典中，形成映射关系
    /// </summary>
    /// <param name="playerState">玩家的状态</param>
    public void RegisterState(IPlayerState playerState)
    {
        states[playerState.StateType] = playerState;
    }

    /// <summary>
    /// 按类型取已注册的状态实例，找不到返回 null。
    /// </summary>
    public T GetState<T>() where T : class, IPlayerState
    {
        foreach (var s in states.Values)
            if (s is T result) return result;
        return null;
    }

    /// <summary>
    /// 初始化状态
    /// </summary>
    /// <param name="initialState">初始状态</param>
    public void Initialize(PlayerStateType initialState)
    {
        if (states.TryGetValue(initialState, out IPlayerState state))
        {
            currentState = state;
            currentState.Enter();
        }
    }

    /// <summary>
    /// 切换状态
    /// </summary>
    /// <param name="stateType">目标状态</param>
    public void TransitionTo(PlayerStateType stateType)
    {
        //先检查这个状态有没有进行注册，没有则直接返回
        if (!states.TryGetValue(stateType, out IPlayerState newState))
        {
            Debug.LogWarning($"状态{stateType}未注册");
            return;
        }

        //如果当前状态无法转换到目标状态，则直接返回
        if (currentState != null && !currentState.CanTransitionTo(stateType))
        {
            if (player.EnableStateDebugLogs)
            {
                Debug.Log($"[PlayerStateMachine] 阻止切换: {currentState.StateType} -> {stateType}", player);
            }
            return;
        }

        PlayerStateType oldStateType = CurrentStateType;
        currentState?.Exit();
        currentState = newState;
        if (player.EnableStateDebugLogs)
        {
            Debug.Log($"[PlayerStateMachine] 状态切换: {oldStateType} -> {stateType}", player);
        }
        currentState.Enter();
    }

    /// <summary>
    /// 先尝试是否能够转换到目标状态
    /// </summary>
    /// <param name="stateType"></param>
    /// <returns></returns>
    public bool TryTransitionTo(PlayerStateType stateType)
    {
        if (currentState != null && !currentState.CanTransitionTo(stateType))
        {
            return false;
        }
        TransitionTo(stateType);
        return true;
    }

    /// <summary>
    /// 将逐帧逻辑转发给当前状态。
    /// </summary>
    public void Update()
    {
        currentState?.Update();
    }

    /// <summary>
    /// 将物理帧逻辑转发给当前状态。
    /// </summary>
    public void FixedUpdate()
    {
        currentState?.FixedUpdate();
    }
}
