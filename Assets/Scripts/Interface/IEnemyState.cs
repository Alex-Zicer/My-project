using UnityEngine;

/// <summary>
/// 敌人状态接口
/// </summary>
public interface IEnemyState
{
    /// <summary>
    /// 进入状态时调用
    /// </summary>
    void Enter();

    /// <summary>
    /// 退出状态时调用
    /// </summary>
    void Exit();

    /// <summary>
    /// 每帧更新
    /// </summary>
    void Update();

    /// <summary>
    /// 固定帧率更新
    /// </summary>
    void FixedUpdate();
}