using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public interface ICharacterController
{
    event System.Action OnJump; //跳跃事件
    event System.Action OnAttack; //攻击事件
    event System.Action OnDash;//冲刺事件
}
