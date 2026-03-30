/// <summary>
/// 敌人状态类型枚举，顺序不影响逻辑。
/// </summary>
public enum EnemyStateType
{
    Patrol,
    Chase,
    Attack,
    Hurt,
    Dead
}

/// <summary>
/// 敌人状态接口，对齐玩家状态机设计：含 StateType 和 CanTransitionTo。
/// </summary>
public interface IEnemyState
{
    EnemyStateType StateType { get; }

    void Enter();
    void Exit();
    void Update();
    void FixedUpdate();

    /// <summary>
    /// 当前状态是否允许转换到目标状态（白名单机制）。
    /// </summary>
    bool CanTransitionTo(EnemyStateType targetState);
}
