using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerData : ScriptableObject
{
    [Header("玩家的基本属性")]
    public float maxHealth;//最大生命值
    public float moveSpeed;//移动速度
    public float moveSpeedMultiplier;//移动加速度，确保人物不会突然转向
}
