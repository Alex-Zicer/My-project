/// <summary>
/// 玩家逻辑状态枚举。
/// 这里只描述会影响输入接收、物理控制和可中断规则的代码状态，
/// 不直接镜像 Animator 中的所有表现细节状态。
/// </summary>
public enum PlayerStateType
{
    Movement,
    Jump,
    Fall,
    Land,
    WallSlide,
    Dash,
    Action,
    Hurt,
    Dead
}

/// <summary>
/// 玩家跳跃类型。
/// 用于在进入 JumpState 时区分普通跳、二段跳和墙跳。
/// </summary>
public enum PlayerJumpKind
{
    None,
    Normal,
    Double,
    Wall
}

/// <summary>
/// 玩家动作类型。
/// 当前只承接 Slash，后续可扩展突刺、远程攻击等动作。
/// </summary>
public enum PlayerActionKind
{
    None,
    Slash
}

/// <summary>
/// 玩家状态接口。
/// 所有玩家状态都必须实现进入、退出、逐帧更新和切换判断。
/// </summary>
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

