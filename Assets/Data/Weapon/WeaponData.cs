using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NewWeaponData", menuName = "Data/WeaponData")]
public class WeaponData : ScriptableObject
{
    [Header("基础信息")]
    public string weaponName;
    public int attackComboCount;

    [Header("每段攻击数据")]
    public AttackData[] attackData;

    [Header("动画")]
    public string attackAnimationPrefix;
}

[System.Serializable]
public class AttackData
{
    public float damage;
    public float duration;

    [Range(0f, 1f)] public float hitStartTime;
    [Range(0f, 1f)] public float hitEndTime;

    public float attackRange;
    public Vector2 attackOffset;
    public AudioClip attackSound;
}
