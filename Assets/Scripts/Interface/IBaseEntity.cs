using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface IBaseEntity
{
    float HorizontalSpeed { get; } //对应于水平移动速度
    float VerticalSpeed { get; } //对应于垂直移动速度
    bool IsDead { get; } //对应于是否死亡
    bool IsGrounded { get; } //对应于是否在地面上

    float FacingDirection { get; } //对应于面朝的方向，-1表示向左，1表示向右
}
