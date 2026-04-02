using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerController : MonoBehaviour, IDamageable, ICharacterController
{
    [Header("数据引用")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private WeaponData defaultWeapon;

    [Header("组件引用")]
    [SerializeField] private Health health;
    private PlayerControls inputActions;//玩家输入系统的引用，使用Unity的新输入系统来处理玩家的输入
    [SerializeField] private LayerMask enemyLayer;

    [Header("跳跃设置")]
    [Tooltip("地面层，用于检测玩家是否站在地面上")]
    public LayerMask groundLayer;
    [Tooltip("放置一个空物体，地面检测点")]
    public Transform groundCheck;
    [Tooltip("地面检测的范围大小")]
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [Tooltip("相对于中心点的偏移，确保检测点位于玩家脚下")]
    public float groundCheckOffset = 0.1f;
    [Tooltip("跳跃段数")]
    public int canJumpCount = 2;

    #region 接口事件
    //用于ICharacterController接口的事件，其他系统可以订阅这些事件来响应玩家的跳跃和着陆行为
    public event Action OnJump;
    public event Action OnAttack;
    public event Action OnHit;       // 玩家受击事件（当前不用于相机反馈）。
    public event Action OnAttackHit; // 攻击命中敌人事件（用于帧冻结 + 镜头抖动）。

    /// <summary>
    /// 供 PlayerAttackState 调用，广播“攻击命中敌人”反馈事件。
    /// </summary>
    public void NotifyAttackHit() => OnAttackHit?.Invoke();

    #endregion

    #region 公开属性状态类访问

    public Vector2 MoveInput { get; private set; }
    public PlayerStateMachine StateMachine { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public PlayerData PlayerData => playerData;
    public Animator animator { get; private set; }
    public WeaponData CurrentWeapon { get; private set; }
    public LayerMask EnemyLayer => enemyLayer;
    public float FacingDirection => transform.localScale.x >= 0f ? 1f : -1f;

    #endregion

    public float HorizontalSpeed => Mathf.Abs(Rb.velocity.x);
    public float VerticalSpeed => Rb.velocity.y;
    public bool IsGround { get; private set; }
    public bool IsDead => StateMachine.CurrentStateType == PlayerStateType.Dead;
    public bool IsInputEnabled => inputActions != null && inputActions.Player.enabled;


    private void Awake()
    {
        //获取组件
        Rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        inputActions = new PlayerControls();
        Rb.freezeRotation = true; // 冻结旋转，确保玩家不会因为物理碰撞而旋转

        CurrentWeapon = defaultWeapon;

        //初始化玩家血量
        health = GetComponent<Health>();
        health.maxHealth = playerData.maxHealth;
        health.currentHealth = health.maxHealth;

        //初始化状态机
        InitializeStateMachine();
    }

    /// <summary>
    /// 初始化状态机，并把每个状态都注册进状态机里
    /// </summary>
    private void InitializeStateMachine()
    {
        StateMachine = new PlayerStateMachine(this);

        StateMachine.RegisterState(new PlayerMovementState(this));
        StateMachine.RegisterState(new PlayerJumpState(this));
        StateMachine.RegisterState(new PlayerFallState(this));
        StateMachine.RegisterState(new PlayerLandState(this));
        StateMachine.RegisterState(new PlayerAttackState(this));
        StateMachine.RegisterState(new PlayerHurtState(this));
        StateMachine.RegisterState(new PlayerDeadState(this));

        StateMachine.Initialize(PlayerStateType.Movement);//设置待机状态为初始状态
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        //订阅跳跃事件，当玩家按下跳跃键时调用OnJumpPerformed方法
        inputActions.Player.Jump.performed += OnJumpPerformed;
        //订阅攻击事件，当玩家按下攻击键时调用OnAttackPerformed方法
        inputActions.Player.Attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();

        //取消订阅跳跃事件，防止内存泄漏
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        //取消订阅攻击事件，防止内存泄漏
        inputActions.Player.Attack.performed -= OnAttackPerformed;
    }

    // Update is called once per frame
    void Update()
    {
        // 输入可能在暂停等场景被禁用；禁用时强制为 0，避免残留输入驱动角色。
        if (IsInputEnabled)
        {
            // 获取玩家的输入，更新移动向量
            MoveInput = inputActions.Player.Move.ReadValue<Vector2>();
        }
        else
        {
            MoveInput = Vector2.zero;
        }

        CheckGroundStatus();
        StateMachine.Update();
    }

    private void FixedUpdate()
    {
        StateMachine.FixedUpdate();
    }

    /// <summary>
    /// 检测人物是否站在地面上
    /// </summary>
    private void CheckGroundStatus()
    {
        //使用OverlapBox检测玩家是否在地面上，groundCheck是一个空物体，放置在玩家脚下，检测范围为0.2f，检测的层为groundLayer
        IsGround = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    /// <summary>
    /// 实现玩家跳跃功能的方法，当玩家按下跳跃键时被调用。它通过设置刚体的垂直速度来实现跳跃效果。
    /// </summary>
    /// <param name="context">用来实现事件系统的参数</param>
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        PlayerStateType currentType = StateMachine.CurrentStateType;
        bool inJumpableState = currentType == PlayerStateType.Movement ||
                               currentType == PlayerStateType.Jump ||
                               currentType == PlayerStateType.Fall;
        if (!inJumpableState) return;

        var jumpState = StateMachine.CurrentState as PlayerJumpState
                     ?? StateMachine.GetState<PlayerJumpState>();
        if (jumpState != null && jumpState.TryConsumeJump())
        {
            StateMachine.TransitionTo(PlayerStateType.Jump);
            OnJump?.Invoke();
        }
    }

    /// <summary>
    /// 处理玩家按下攻击键时的函数
    /// </summary>
    /// <param name="context">用来实现事件系统的参数</param>
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        PlayerStateType current = StateMachine.CurrentStateType;
        //如果当前状态就是攻击状态，则进行下一段攻击
        if (current == PlayerStateType.Attack)
        {
            bool queued = (StateMachine.CurrentState as PlayerAttackState)?.QueueNextAttack() == true;
            if (queued)
            {
                OnAttack?.Invoke();
            }
        }
        else if (IsGround && current != PlayerStateType.Hurt && current != PlayerStateType.Dead)
        {
            StateMachine.TransitionTo(PlayerStateType.Attack);//进行第一段攻击
            OnAttack?.Invoke();
        }
    }

    /// <summary>
    /// 计算受到的伤害
    /// </summary>
    /// <param name="rawDamage">受到的原始伤害</param>
    public void TakeDamage(float rawDamage)
    {
        if (IsDead) return;

        float finalDamage = Mathf.Max(rawDamage - playerData.defence, 0);
        health.UpdateHealth(finalDamage);

        if (health.currentHealth > 0)
        {
            OnHit?.Invoke();
            StateMachine.TransitionTo(PlayerStateType.Hurt);
        }
        else
        {
            StateMachine.TransitionTo(PlayerStateType.Dead);
        }
    }

    /// <summary>
    /// 获取武器数据
    /// </summary>
    /// <param name="weapon">当前武器</param>
    public void EquipWeapon(WeaponData weapon)
    {
        CurrentWeapon = weapon;
    }

    /// <summary>
    /// 外部（如暂停系统）开关玩家输入。仅控制 InputActionMap，不影响组件启用状态。
    /// </summary>
    public void SetInputEnabled(bool enabled)
    {
        if (inputActions == null) return;

        if (enabled)
        {
            // 若组件本身被禁用（如死亡禁用 PlayerController），不要在这里强行启用输入。
            if (!isActiveAndEnabled) return;
            if (!inputActions.Player.enabled) inputActions.Player.Enable();
        }
        else
        {
            if (inputActions.Player.enabled) inputActions.Player.Disable();
            MoveInput = Vector2.zero;
        }
    }

    /// <summary>
    /// 获取攻击朝向
    /// </summary>
    /// <param name="attackOffset">攻击偏移点</param>
    /// <returns></returns>
    public Vector2 GetAttackWorldPosition(Vector2 attackOffset)
    {
        Vector2 facingOffset = attackOffset;
        facingOffset.x *= FacingDirection;
        return (Vector2)transform.position + facingOffset;
    }

    /// <summary>
    /// 尝试获取攻击预览
    /// </summary>
    /// <param name="attackPos">攻击点</param>
    /// <param name="attackRange">攻击范围i</param>
    /// <param name="isActiveAttack">是否处于攻击状态</param>
    /// <returns></returns>
    private bool TryGetAttackPreview(out Vector2 attackPos, out float attackRange, out bool isActiveAttack)
    {
        attackPos = Vector2.zero;
        attackRange = 0f;
        isActiveAttack = false;

        if (Application.isPlaying && StateMachine?.CurrentState is PlayerAttackState attackState &&
            attackState.TryGetDebugAttackGizmo(out attackPos, out attackRange))
        {
            isActiveAttack = true;
            return true;
        }

        WeaponData previewWeapon = Application.isPlaying ? CurrentWeapon : defaultWeapon;
        if (previewWeapon == null || previewWeapon.attackData == null || previewWeapon.attackData.Length == 0 ||
            previewWeapon.attackData[0] == null)
        {
            return false;
        }

        AttackData previewData = previewWeapon.attackData[0];
        attackPos = GetAttackWorldPosition(previewData.attackOffset);
        attackRange = previewData.attackRange;
        return true;
    }

    /// <summary>
    /// 画出攻击判定范围
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        if (!TryGetAttackPreview(out Vector2 attackPos, out float attackRange, out bool isActiveAttack))
        {
            return;
        }

        Gizmos.color = isActiveAttack ? Color.red : Color.yellow;
        Gizmos.DrawLine(transform.position, attackPos);
        Gizmos.DrawWireSphere(attackPos, attackRange);
    }

}
