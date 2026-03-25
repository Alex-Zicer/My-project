using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色动画基类。
/// 作用：为具体角色动画脚本提供统一的 Animator 与控制器引用入口。
/// 子类可以在此基础上监听控制器事件（跳跃、攻击等）并驱动动画参数。
/// </summary>
public class BaseCharaterAnimation : MonoBehaviour
{
    /// <summary>
    /// 角色动画控制器组件。
    /// 约定由子类在初始化阶段赋值并使用。
    /// </summary>
    protected Animator anim;

    /// <summary>
    /// 角色控制器接口抽象。
    /// 通过接口而非具体类耦合，便于玩家/敌人共用动画驱动逻辑。
    /// </summary>
    protected ICharacterController controller;

    // 生命周期预留：供子类按需覆写初始化逻辑。
    void Start()
    {
    }

    // 生命周期预留：供子类按需覆写逐帧动画同步逻辑。
    void Update()
    {
    }
}
