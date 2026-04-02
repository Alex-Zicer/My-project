using System.Collections.Generic;
using UnityEngine;

public class PlayerAttackState : PlayerStateBase
{
    public override PlayerStateType StateType => PlayerStateType.Attack;

    // 当前武器。
    private WeaponData currentWeapon;

    // 当前攻击段索引。
    private int attackIndex;

    // 当前段计时器。
    private float attackTimer;

    // 是否已预输入下一段。
    private bool nextAttackQueued;

    // 当前段是否已结算过伤害。
    private bool hasDealDamage;

    // 当前段已命中的目标缓存。
    private readonly List<Collider2D> hitTargets;

    // 连招动画状态哈希。
    private static readonly int[] AttackHashes =
    {
        Animator.StringToHash("AttackCombo.Attack1"),
        Animator.StringToHash("AttackCombo.Attack2")
    };

    /// <summary>
    /// 构造攻击状态。
    /// </summary>
    /// <param name="player">玩家控制器。</param>
    public PlayerAttackState(PlayerController player) : base(player)
    {
        hitTargets = new List<Collider2D>();
    }

    /// <summary>
    /// 进入攻击状态并启动第一段攻击。
    /// </summary>
    public override void Enter()
    {
        attackIndex = 0;
        currentWeapon = player.CurrentWeapon;
        nextAttackQueued = false;

        if (!TryGetAttackData(out _, true))
        {
            ReturnToMovementState();
            return;
        }

        StartAttack();
    }

    /// <summary>
    /// 退出攻击状态时清理运行时缓存。
    /// </summary>
    public override void Exit()
    {
        hasDealDamage = false;
        hitTargets.Clear();
    }

    /// <summary>
    /// 攻击期间保留平滑移动。
    /// </summary>
    public override void FixedUpdate()
    {
        SmoothSpeed();
    }

    /// <summary>
    /// 推进攻击流程、命中窗口和连段切换。
    /// </summary>
    public override void Update()
    {
        attackTimer += Time.deltaTime;

        if (!TryGetAttackData(out AttackData data, true))
        {
            ReturnToMovementState();
            return;
        }

        if (data.duration <= 0f)
        {
            Debug.LogWarning($"武器 {currentWeapon.name} 第 {attackIndex + 1} 段攻击的 duration 必须大于 0。", player);
            ReturnToMovementState();
            return;
        }

        // 攻击状态下也允许根据输入调整朝向。
        FlipCharacter();

        float normalizeTime = attackTimer / data.duration;

        // 命中窗口只结算一次。
        if (!hasDealDamage && normalizeTime >= data.hitStartTime && normalizeTime <= data.hitEndTime)
        {
            hasDealDamage = true;
            DetectHit(data);
        }

        // 连段窗口结束后决定是否进入下一段。
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
    /// 在连段窗口内缓存下一段输入。
    /// </summary>
    /// <returns>缓存成功返回 true。</returns>
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
    /// 攻击过程中仅允许受击和死亡打断。
    /// </summary>
    /// <param name="state">目标状态。</param>
    /// <returns>允许切换返回 true。</returns>
    public override bool CanTransitionTo(PlayerStateType state)
    {
        if (!TryGetAttackData(out AttackData data))
        {
            return true;
        }

        if (data.duration <= 0f || attackTimer >= data.duration)
        {
            return true;
        }

        return state == PlayerStateType.Hurt || state == PlayerStateType.Dead;
    }

    /// <summary>
    /// 提供当前攻击范围用于 Gizmos 预览。
    /// </summary>
    /// <param name="attackPos">攻击中心点。</param>
    /// <param name="attackRange">攻击半径。</param>
    /// <returns>可预览返回 true。</returns>
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
    /// 启动当前段攻击。
    /// 动作音效改由动画关键帧事件触发，不在这里直接播放。
    /// </summary>
    private void StartAttack()
    {
        attackTimer = 0f;
        nextAttackQueued = false;
        hasDealDamage = false;
        hitTargets.Clear();

        if (!TryGetAttackData(out _, true))
        {
            ReturnToMovementState();
            return;
        }

        int hashIndex = Mathf.Min(attackIndex, AttackHashes.Length - 1);
        anim.CrossFade(AttackHashes[hashIndex], 0.05f);
    }

    /// <summary>
    /// 在命中窗口内检测并结算伤害。
    /// </summary>
    /// <param name="data">当前段攻击数据。</param>
    private void DetectHit(AttackData data)
    {
        Vector2 attackPos = player.GetAttackWorldPosition(data.attackOffset);
        Collider2D[] hits = Physics2D.OverlapCircleAll(attackPos, data.attackRange, player.EnemyLayer);

        for (int i = 0; i < hits.Length; i++)
        {
            Collider2D hit = hits[i];
            if (hitTargets.Contains(hit))
            {
                continue;
            }

            if (hit.TryGetComponent<IDamageable>(out IDamageable damageable))
            {
                // 先结算伤害，再把该目标记入已命中缓存，避免同一段重复结算。
                damageable.TakeDamage(data.damage);
                hitTargets.Add(hit);
            }
        }

        if (hitTargets.Count > 0)
        {
            // 命中结果音效当前不在这里触发，先统一由动画关键帧事件负责动作音效。
            // 后续需要命中音效时，可在此处恢复 data.hitSfxEvent 的播放逻辑（保持空值判断）。

            // 只要本段攻击命中了至少一个目标，就广播一次命中反馈事件。
            player.NotifyAttackHit();
        }
    }

    /// <summary>
    /// 获取当前段攻击配置。
    /// </summary>
    /// <param name="data">输出攻击数据。</param>
    /// <param name="logError">是否打印错误日志。</param>
    /// <returns>获取成功返回 true。</returns>
    private bool TryGetAttackData(out AttackData data, bool logError = false)
    {
        data = null;

        if (currentWeapon == null)
        {
            if (logError)
            {
                Debug.LogWarning("当前没有装备武器，无法进入攻击状态。", player);
            }
            return false;
        }

        if (currentWeapon.attackData == null || currentWeapon.attackData.Length == 0)
        {
            if (logError)
            {
                Debug.LogWarning($"武器 {currentWeapon.name} 未配置 attackData，无法进入攻击状态。", player);
            }
            return false;
        }

        if (attackIndex < 0 || attackIndex >= currentWeapon.attackData.Length)
        {
            if (logError)
            {
                Debug.LogWarning(
                    $"武器 {currentWeapon.name} 攻击段索引越界：index={attackIndex}, length={currentWeapon.attackData.Length}。",
                    player);
            }
            return false;
        }

        data = currentWeapon.attackData[attackIndex];
        if (data == null)
        {
            if (logError)
            {
                Debug.LogWarning($"武器 {currentWeapon.name} 第 {attackIndex + 1} 段攻击数据为空。", player);
            }
            return false;
        }

        return true;
    }

    /// <summary>
    /// 获取可用连击的最大索引。
    /// </summary>
    /// <returns>最大索引，失败返回 -1。</returns>
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
