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
    private readonly List<Collider2D> hitTargets;

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
    /// 进入攻击状态：初始化连段并开始第一段攻击。
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

    public override void Exit()
    {
        hasDealDamage = false;
        hitTargets.Clear();
    }

    /// <summary>
    /// 攻击期间保持读取输入：
    /// 1. 允许角色在攻击时继续移动/减速；
    /// 2. 避免“进入攻击后沿用旧速度滑行”。
    /// </summary>
    public override void FixedUpdate()
    {
        SmoothSpeed();
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

        // 攻击状态下也允许根据输入调整朝向。
        FlipCharacter();

        float normalizeTime = attackTimer / data.duration;

        // 在伤害判定窗口内只结算一次伤害。
        if (!hasDealDamage && normalizeTime >= data.hitStartTime && normalizeTime <= data.hitEndTime)
        {
            hasDealDamage = true;
            DetectHit(data);
        }

        // 连段窗口结束后，若没有预输入下一段则返回移动/下落状态。
        if (attackTimer >= data.duration * 0.8f)
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
    /// 预输入下一段攻击（连段）。
    /// </summary>
    public bool QueueNextAttack()
    {
        if (!TryGetAttackData(out AttackData data)) return false;
        if (data.duration <= 0f) return false;

        float normalizeTime = attackTimer / data.duration;
        if (normalizeTime < 0.8f && attackIndex < GetMaxComboIndex())
        {
            nextAttackQueued = true;
            return true;
        }

        return false;
    }

    public override bool CanTransitionTo(PlayerStateType state)
    {
        if (!TryGetAttackData(out AttackData data)) return true;
        if (data.duration <= 0f || attackTimer >= data.duration) return true;

        // 攻击过程中只允许被受击/死亡打断。
        return state == PlayerStateType.Hurt || state == PlayerStateType.Dead;
    }

    /// <summary>
    /// 供 Gizmos 预览当前攻击范围。
    /// </summary>
    public bool TryGetDebugAttackGizmo(out Vector2 attackPos, out float attackRange)
    {
        attackPos = Vector2.zero;
        attackRange = 0f;

        if (!TryGetAttackData(out AttackData data)) return false;

        attackPos = player.GetAttackWorldPosition(data.attackOffset);
        attackRange = data.attackRange;
        return true;
    }

    private void StartAttack()
    {
        attackTimer = 0f;
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

        if (data.attackSound != null)
        {
            AudioSource.PlayClipAtPoint(data.attackSound, player.transform.position);
        }
    }

    private void DetectHit(AttackData data)
    {
        Vector2 attackPos = player.GetAttackWorldPosition(data.attackOffset);
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, data.attackRange, player.EnemyLayer);

        foreach (Collider2D hit in hits)
        {
            // 同一段攻击只对同一目标结算一次。
            if (hitTargets.Contains(hit)) continue;

            if (hit.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                damageable.TakeDamage(data.damage);
                hitTargets.Add(hit);
            }
        }

        if (hitTargets.Count > 0)
        {
            player.NotifyAttackHit();
        }
    }

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
                Debug.LogWarning(
                    $"武器 {currentWeapon.name} 的攻击段索引越界：index={attackIndex}，length={currentWeapon.attackData.Length}。");
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
