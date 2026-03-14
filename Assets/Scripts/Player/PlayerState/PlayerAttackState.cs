using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Attack;

    private WeaponData currentWeapon;
    private int attackIndex;
    private float attackTimer;
    private bool nextAttackQueued;
    private bool hasDealDamage;
    private List<Collider2D> hitTargets;
    private static readonly int[] AttackHashes =
    {
        Animator.StringToHash("AttackCombo.Attack1"),
        Animator.StringToHash("AttackCombo.Attack2")
    };
    public PlayerAttackState(PlayerController player) : base(player)
    {
        hitTargets = new List<Collider2D>();
    }

    /// <summary>
    /// 初始化攻击段数
    /// </summary>
    public override void Enter()
    {
        attackIndex = 0;
        currentWeapon = player.CurrentWeapon;
        nextAttackQueued = false;
        StartAttack();
    }

    /// <summary>
    /// 离开攻击状态时，重置攻击段数
    /// </summary>
    public override void Exit()
    {
        hasDealDamage = false;
        hitTargets.Clear();
    }

    /// <summary>
    /// 开始攻击，并设置攻击动画参数
    /// </summary>
    private void StartAttack()
    {
        attackTimer = 0;
        nextAttackQueued = false;
        hasDealDamage = false;
        hitTargets.Clear();

        AttackData data = currentWeapon.attackData[attackIndex];

        int hashIndex = Mathf.Min(attackIndex, AttackHashes.Length - 1);
        anim.CrossFade(AttackHashes[hashIndex], 0.05f);

        //播放攻击音效
        if (data.attackSound != null)
        {
            AudioSource.PlayClipAtPoint(data.attackSound, player.transform.position);
        }
    }

    public override void Update()
    {
        attackTimer += Time.deltaTime;

        AttackData data = currentWeapon.attackData[attackIndex];
        float normalizeTime = attackTimer / data.duration;

        //在攻击动画的特定时间点检测伤害
        if (!hasDealDamage && normalizeTime >= data.hitStartTime && normalizeTime <= data.hitEndTime)
        {
            hasDealDamage = true;
            DetectHit(data);
        }

        //检测是否进行下一段攻击，没有则回到移动模式
        if (attackTimer >= data.duration * 0.8)
        {
            if (nextAttackQueued && attackIndex < currentWeapon.attackComboCount - 1)
            {
                attackIndex++;
                StartAttack();
            }
            else
            {
                ReturnToMovementState();
            }
        }
    }

    /// <summary>
    /// 伤害处理
    /// </summary>
    private void DetectHit(AttackData data)
    {
        Vector2 attackPos = (Vector2)player.transform.position + data.attackOffset;

        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, data.attackRange, player.EnemyLayer);

        foreach (var hit in hits)
        {
            //判断此次攻击有没有命中过这个目标，以防造成多次伤害
            if (!hitTargets.Contains(hit))
            {
                if (hit.TryGetComponent<IDamageable>(out IDamageable damageable))
                {
                    damageable.TakeDamage(data.damage);
                    hitTargets.Add(hit);
                }
            }
        }
    }

    /// <summary>
    /// 预输入设置
    /// </summary>
    public void QueueNextAttack()
    {
        AttackData data = currentWeapon.attackData[attackIndex];
        float normalizeTime = attackTimer / data.duration;

        if (normalizeTime < 0.8f)
        {
            nextAttackQueued = true;
        }
    }

    /// <summary>
    /// 检测能否转换到对应状态
    /// </summary>
    /// <param name="state">目标状态</param>
    /// <returns></returns>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        //数据异常直接返回true切换到其他状态
        if (currentWeapon == null || currentWeapon.attackData == null || attackIndex < 0 || attackIndex > currentWeapon.attackData.Length)
        {
            return true;
        }

        AttackData data = currentWeapon?.attackData[attackIndex];
        //攻击结束之后可以转换到任何状态
        if (attackTimer >= data.duration)
        {
            return true;
        }
        //攻击状态过程中只能被受击和死亡状态打断
        return state == PlayerStateType.Hurt || state == PlayerStateType.Dead;
    }
}
