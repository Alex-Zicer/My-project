using System.Collections;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 基础实体状态接口。
/// 约束“可被动画、状态机或 UI 读取”的通用实体状态信息。
/// </summary>
public interface IBaseEntity
{
    /// <summary>
    /// 当前水平速度（通常取绝对值或沿 X 轴速度分量）。
    /// 常用于移动动画混合参数。
    /// </summary>
    float HorizontalSpeed { get; }

    /// <summary>
    /// 当前垂直速度（通常用于跳跃/下落状态判断）。
    /// </summary>
    float VerticalSpeed { get; }

    /// <summary>
    /// 实体是否已死亡。
    /// 为 true 时通常应屏蔽移动、攻击和交互输入。
    /// </summary>
    bool IsDead { get; }

    /// <summary>
    /// 实体是否处于地面接触状态。
    /// 常用于跳跃许可、落地判定和地面动画切换。
    /// </summary>
    bool IsGround { get; }
}
