using UnityEngine;

/// <summary>
/// 状态机类，用于管理状态的切换
/// </summary>
public class StateMachine
{
    private IEnemyState currentState;

    /// <summary>
    /// 改变状态
    /// </summary>
    /// <param name="newState">新状态</param>
    public void ChangeState(IEnemyState newState)
    {
        // 退出当前状态
        if (currentState != null)
        {
            currentState.Exit();
        }

        // 设置新状态
        currentState = newState;

        // 进入新状态
        if (currentState != null)
        {
            currentState.Enter();
        }
    }

    /// <summary>
    /// 更新当前状态
    /// </summary>
    public void Update()
    {
        if (currentState != null)
        {
            currentState.Update();
        }
    }

    /// <summary>
    /// 固定帧率更新当前状态
    /// </summary>
    public void FixedUpdate()
    {
        if (currentState != null)
        {
            currentState.FixedUpdate();
        }
    }

    /// <summary>
    /// 获取当前状态
    /// </summary>
    public IEnemyState GetCurrentState()
    {
        return currentState;
    }
}