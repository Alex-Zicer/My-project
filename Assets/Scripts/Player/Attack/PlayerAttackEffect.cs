using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 挂载在攻击特效预制体上的简易伤害判定脚本。
/// 特效生成后自动开启 Trigger Collider，在命中窗口结束前检测进入范围的敌人，
/// 随后关闭判定并在生命周期结束后自动销毁。
/// </summary>
[RequireComponent(typeof(Collider2D))]
public class PlayerAttackEffect : MonoBehaviour
{
    [Header("生成设置")]
    [SerializeField] private Vector2 spawnOffset = new Vector2(0.8f, 0f);
    [SerializeField] private bool followSpawnPoint;

    [Header("伤害设置")]
    [SerializeField] private float damage = 1f;
    [SerializeField] private float hitboxDuration = 0.08f;
    [SerializeField] private float effectLifetime = 0.18f;
    [SerializeField] private Collider2D hitboxCollider;

    private readonly HashSet<int> _hitTargetIds = new HashSet<int>();

    private PlayerController _owner;
    private Transform _spawnPoint;
    private Vector3 _initialScale;
    private float _lifeTimer;
    private float _destroyDelay;
    private bool _isInitialized;

    /// <summary>
    /// 缓存初始缩放和判定 Collider，并确保生成前 Hitbox 处于关闭状态。
    /// </summary>
    private void Awake()
    {
        _initialScale = transform.localScale;

        if (hitboxCollider == null)
        {
            hitboxCollider = GetComponent<Collider2D>();
        }

        hitboxCollider.isTrigger = true;
        hitboxCollider.enabled = false;
    }

    /// <summary>
    /// 由角色在生成特效后调用，初始化特效归属者、朝向、命中窗口与自毁时间。
    /// </summary>
    /// <param name="owner">攻击发起者。</param>
    /// <param name="spawnPoint">特效生成参考点。</param>
    public void Initialize(PlayerController owner, Transform spawnPoint)
    {
        _owner = owner;
        _spawnPoint = spawnPoint;
        _lifeTimer = 0f;
        _destroyDelay = Mathf.Max(effectLifetime, hitboxDuration);
        _isInitialized = true;
        damage = ResolveDamage(owner);

        _hitTargetIds.Clear();
        ApplySpawnPose();
        EnableHitbox();
    }

    /// <summary>
    /// 维护命中窗口与生命周期；需要跟随角色时，同时刷新位置和朝向。
    /// </summary>
    private void Update()
    {
        if (!_isInitialized)
        {
            return;
        }

        if (followSpawnPoint && _owner != null)
        {
            ApplySpawnPose();
        }

        _lifeTimer += Time.deltaTime;

        if (hitboxCollider.enabled && _lifeTimer >= hitboxDuration)
        {
            DisableHitbox();
        }

        if (_lifeTimer >= _destroyDelay)
        {
            DestroyEffect();
        }
    }

    /// <summary>
    /// 启用伤害判定 Collider。
    /// 如果后续想精确卡帧，可改为由特效自身动画事件调用。
    /// </summary>
    public void EnableHitbox()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = true;
        }
    }

    /// <summary>
    /// 关闭伤害判定 Collider，避免特效残留时继续判定伤害。
    /// </summary>
    public void DisableHitbox()
    {
        if (hitboxCollider != null)
        {
            hitboxCollider.enabled = false;
        }
    }

    /// <summary>
    /// 销毁特效对象。
    /// 销毁前会主动关闭 Hitbox，保证不会留下残余判定。
    /// </summary>
    public void DestroyEffect()
    {
        DisableHitbox();
        Destroy(gameObject);
    }

    /// <summary>
    /// 命中时对敌人造成伤害，并向玩家广播一次命中反馈事件。
    /// 单个特效实例只会对同一目标结算一次伤害。
    /// </summary>
    /// <param name="other">进入判定范围的 Collider。</param>
    private void OnTriggerEnter2D(Collider2D other)
    {
        if (!_isInitialized || !hitboxCollider.enabled || _owner == null)
        {
            return;
        }

        if (other.transform.root == _owner.transform.root)
        {
            return;
        }

        IDamageable damageable = other.GetComponentInParent<IDamageable>();
        if (damageable == null || damageable is not Component damageableComponent)
        {
            return;
        }

        int targetInstanceId = damageableComponent.GetInstanceID();
        if (!_hitTargetIds.Add(targetInstanceId))
        {
            return;
        }

        damageable.TakeDamage(damage);
        _owner.NotifyAttackHit();
    }

    /// <summary>
    /// 根据玩家面向和预设偏移摆放特效位置，并镜像特效朝向。
    /// 正的 spawnOffset.x 表示“朝前方”的偏移。
    /// </summary>
    private void ApplySpawnPose()
    {
        if (_owner == null)
        {
            return;
        }

        Transform origin = _spawnPoint != null ? _spawnPoint : _owner.transform;
        float facingDirection = _owner.FacingDirectionX;
        Vector3 offset = new Vector3(spawnOffset.x * facingDirection, spawnOffset.y, 0f);

        transform.position = origin.position + offset;
        transform.localScale = new Vector3(
            Mathf.Abs(_initialScale.x) * Mathf.Sign(_owner.transform.localScale.x),
            _initialScale.y,
            _initialScale.z);
    }

    /// <summary>
    /// 从玩家基础数值读取本次攻击伤害，不依赖武器系统。
    /// </summary>
    private float ResolveDamage(PlayerController owner)
    {
        if (owner == null || owner.PlayerData == null)
        {
            return Mathf.Max(damage, 0f);
        }

        return Mathf.Max(owner.PlayerData.attack, 0f);
    }
}