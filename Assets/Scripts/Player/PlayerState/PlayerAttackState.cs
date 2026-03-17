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
        if (!TryGetAttackData(out _, logError: true))
        {
            ReturnToMovementState();
            return;
        }
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

        if (!TryGetAttackData(out AttackData data, logError: true))
        {
            ReturnToMovementState();
            return;
        }

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

        if (!TryGetAttackData(out AttackData data, logError: true))
        {
            ReturnToMovementState();
            return;
        }

        if (data.duration <= 0f)
        {
            Debug.LogWarning($"武器 {currentWeapon.name} 的第 {attackIndex + 1} 段攻击 duration 必须大于 0。");
            ReturnToMovementState();
            return;
        }

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
            int maxComboIndex = GetMaxComboIndex();
            if (nextAttackQueued && attackIndex < maxComboIndex)
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
        Vector2 attackPos = player.GetAttackWorldPosition(data.attackOffset);

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
    public bool QueueNextAttack()
    {
        if (!TryGetAttackData(out AttackData data))
        {
            return false;
        }

        if (data.duration <= 0f)
        {
            return false;
        }

        float normalizeTime = attackTimer / data.duration;

        if (normalizeTime < 0.8f && attackIndex < GetMaxComboIndex())
        {
            nextAttackQueued = true;
            return true;
        }

        return false;
    }

    /// <summary>
    /// 检测能否转换到对应状态
    /// </summary>
    /// <param name="state">目标状态</param>
    /// <returns></returns>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        //数据异常直接返回true切换到其他状态
        if (!TryGetAttackData(out AttackData data))
        {
            return true;
        }

        //攻击结束之后可以转换到任何状态
        if (data.duration <= 0f || attackTimer >= data.duration)
        {
            return true;
        }
        //攻击状态过程中只能被受击和死亡状态打断
        return state == PlayerStateType.Hurt || state == PlayerStateType.Dead;
    }

    /// <summary>
    /// 画出攻击范围
    /// </summary>
    /// <param name="attackPos">攻击位置</param>
    /// <param name="attackRange">攻击范围</param>
    /// <returns>返回一个bool值来判断能否画出攻击范围</returns>
    public bool TryGetDebugAttackGizmo(out Vector2 attackPos, out float attackRange)
    {
        attackPos = Vector2.zero;
        attackRange = 0f;

        if (!TryGetAttackData(out AttackData data))
        {
            return false;
        }

        attackPos = player.GetAttackWorldPosition(data.attackOffset);
        attackRange = data.attackRange;
        return true;
    }

    /// <summary>
    /// 获取武器攻击参数
    /// </summary>
    /// <param name="data">武器数据</param>
    /// <param name="logError">是否输出错误原因</param>
    /// <returns>返回一个bool值来判断是否获取了攻击参数</returns>
    private bool TryGetAttackData(out AttackData data, bool logError = false)
    {
        data = null;

        if (currentWeapon == null)
        {
            if (logError)
            {
                Debug.LogWarning("当前没有装备武器，无法进入攻击状态。");
            }
            return false;
        }

        if (currentWeapon.attackData == null || currentWeapon.attackData.Length == 0)
        {
            if (logError)
            {
                Debug.LogWarning($"武器 {currentWeapon.name} 没有配置 attackData，无法进入攻击状态。");
            }
            return false;
        }

        if (attackIndex < 0 || attackIndex >= currentWeapon.attackData.Length)
        {
            if (logError)
            {
                Debug.LogWarning($"武器 {currentWeapon.name} 的攻击段数越界：index={attackIndex}，length={currentWeapon.attackData.Length}。");
            }
            return false;
        }

        data = currentWeapon.attackData[attackIndex];
        if (data == null)
        {
            if (logError)
            {
                Debug.LogWarning($"武器 {currentWeapon.name} 的第 {attackIndex + 1} 段攻击数据为空。");
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取攻击当前武器的最大组合索引
    /// </summary>
    /// <returns>返回最大攻击组合索引，如果攻击数据无效则返回-1</returns>
    private int GetMaxComboIndex()
    {
        if (currentWeapon == null || currentWeapon.attackData == null || currentWeapon.attackData.Length == 0)
        {
            return -1;
        }

        int comboCount = currentWeapon.attackComboCount > 0
            ? currentWeapon.attackComboCount
            : currentWeapon.attackData.Length;

        return Mathf.Min(comboCount, currentWeapon.attackData.Length) - 1;
    }
}
