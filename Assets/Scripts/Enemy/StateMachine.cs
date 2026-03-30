using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 敌人状态机。对齐玩家状态机设计：预注册、白名单转换、GetState<T> 查询。
/// </summary>
public class EnemyStateMachine
{
    private readonly Dictionary<EnemyStateType, IEnemyState> _states
        = new Dictionary<EnemyStateType, IEnemyState>();

    private IEnemyState _currentState;

    public IEnemyState CurrentState => _currentState;
    public EnemyStateType CurrentStateType => _currentState?.StateType ?? EnemyStateType.Patrol;

    /// <summary>
    /// 注册状态到字典，Awake 时统一注册。
    /// </summary>
    public void RegisterState(IEnemyState state)
    {
        _states[state.StateType] = state;
    }

    /// <summary>
    /// 初始化并进入初始状态。
    /// </summary>
    public void Initialize(EnemyStateType initialState)
    {
        if (!_states.TryGetValue(initialState, out IEnemyState state))
        {
            Debug.LogWarning($"[EnemyStateMachine] 初始状态 {initialState} 未注册");
            return;
        }
        _currentState = state;
        _currentState.Enter();
    }

    /// <summary>
    /// 切换状态，先检查白名单，再 Exit → Enter。
    /// </summary>
    public void TransitionTo(EnemyStateType targetState)
    {
        if (!_states.TryGetValue(targetState, out IEnemyState newState))
        {
            Debug.LogWarning($"[EnemyStateMachine] 状态 {targetState} 未注册");
            return;
        }

        if (_currentState != null && !_currentState.CanTransitionTo(targetState))
            return;

        _currentState?.Exit();
        _currentState = newState;
        _currentState.Enter();
    }

    /// <summary>
    /// 按类型取已注册的状态实例，找不到返回 null。
    /// </summary>
    public T GetState<T>() where T : class, IEnemyState
    {
        foreach (var s in _states.Values)
            if (s is T result) return result;
        return null;
    }

    public void Update()   => _currentState?.Update();
    public void FixedUpdate() => _currentState?.FixedUpdate();
}
