using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "PlayerData", menuName = "PlayerData")]
public class PlayerData : ScriptableObject
{
    [Header("玩家的基本属性")]
    public float maxHealth;//最大生命值
    public float moveSpeed;//移动速度
    [Tooltip("启动加速度，保证人物不会突然变向")]
    public float moveSpeedMultiplier;//移动加速度，确保人物不会突然转向
    [Tooltip("防御力")]
    public float defence;//玩家防御力

    [Header("玩家的攻击属性")]
    public float atkPower;
    public float atkCoolDown;
}
