using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 角色控制事件接口。
/// 向动画层或其他订阅方暴露关键动作事件，减少对具体控制器实现的直接依赖。
/// </summary>
public interface ICharacterController
{
    /// <summary>
    /// 跳跃动作触发事件。
    /// 建议在角色“真正起跳”的时刻触发，而不是按键按下瞬间。
    /// </summary>
    event System.Action OnJump;

    /// <summary>
    /// 攻击动作触发事件。
    /// 可用于驱动攻击动画、镜头反馈或音效播放。
    /// </summary>
    event System.Action OnAttack;
}
