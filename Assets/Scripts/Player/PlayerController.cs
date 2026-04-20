using UnityEngine;
using UnityEngine.InputSystem;
using System;

[RequireComponent(typeof(PlayerAnimationDriver))]
public class PlayerController : MonoBehaviour, IDamageable, ICharacterController
{
    private const float InitialHealthValue = 5f; // 分段血条初始生命值（1 格 = 1 血）

    [Header("数据引用")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private bool enableStateDebugLogs;

    [Header("组件引用")]
    [SerializeField] private Health health;
    private PlayerControls inputActions;         // Unity 新输入系统
    private PlayerAnimationDriver _animDriver;   // 动画驱动层（唯一写入 Animator 参数的模块）
    private PlayerJumpKind _pendingJumpKind;
    private PlayerActionKind _pendingActionKind;
    private float _nextDashReadyTime;

    [Header("跳跃设置")]
    [Tooltip("地面层，用于检测玩家是否站在地面上")]
    public LayerMask groundLayer;
    [Tooltip("放置一个空物体，地面检测点")]
    public Transform groundCheck;
    [Tooltip("地面检测的范围大小")]
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);

    [Header("墙体检测")]
    [Tooltip("墙体层")]
    public LayerMask wallLayer;
    [Tooltip("检测盒中心相对角色中心的水平偏移，根据玩家朝向自动正负")]
    public float wallCheckOffset = 0.4f;
    [Tooltip("墙体检测范围大小")]
    public Vector2 wallCheckSize = new Vector2(0.1f, 0.8f);

    #region 接口事件
    //用于ICharacterController接口的事件，其他系统可以订阅这些事件来响应玩家的跳跃和着陆行为
    public event Action OnJump;
    public event Action OnAttack;
    public event Action OnHit;       // 玩家受击事件（当前不用于相机反馈）。
    public event Action OnAttackHit; // 攻击命中敌人事件（用于帧冻结 + 镜头抖动）。

    /// <summary>
    /// 供攻击特效命中逻辑调用，广播“攻击命中敌人”反馈事件。
    /// </summary>
    public void NotifyAttackHit() => OnAttackHit?.Invoke();

    #endregion

    #region 公开属性状态类访问

    public Vector2 MoveInput { get; private set; }
    public PlayerStateMachine StateMachine { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public PlayerData PlayerData => playerData;
    public Animator animator { get; private set; }
    public float DefaultGravityScale { get; private set; }
    public bool EnableStateDebugLogs => enableStateDebugLogs;

    #endregion

    public float HorizontalSpeed => Mathf.Abs(Rb.velocity.x);
    public float VerticalSpeed => Rb.velocity.y;
    public bool IsGround { get; private set; }
    public bool IsWall { get; private set; }
    public bool CanWallSlide => IsWall && !IsGround && VerticalSpeed < -0.01f;
    public float FacingDirectionX => transform.localScale.x < 0f ? 1f : -1f;
    // 贴墙且向下下落时的下滑速度绝对值；未贴墙、在地面或上升时为 0
    public float WallDownSpeed => IsWall && !IsGround && VerticalSpeed < 0 ? Mathf.Abs(VerticalSpeed) : 0f;
    public bool IsDead => StateMachine != null && StateMachine.CurrentStateType == PlayerStateType.Dead;
    public bool IsInputEnabled => inputActions != null && inputActions.Player.enabled;


    private void Awake()
    {
        Rb = GetComponent<Rigidbody2D>();
        animator = GetComponent<Animator>();
        _animDriver = GetComponent<PlayerAnimationDriver>();
        inputActions = new PlayerControls();
        Rb.freezeRotation = true;
        DefaultGravityScale = Rb.gravityScale;

        health = GetComponent<Health>();
        health.SetMaxHealth(InitialHealthValue, true);

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
        StateMachine.RegisterState(new PlayerWallSlideState(this));
        StateMachine.RegisterState(new PlayerDashState(this));
        StateMachine.RegisterState(new PlayerActionState(this));
        StateMachine.RegisterState(new PlayerHurtState(this));
        StateMachine.RegisterState(new PlayerDeadState(this));

        StateMachine.Initialize(PlayerStateType.Movement);//设置待机状态为初始状态
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        //订阅跳跃事件，当玩家按下跳跃键时调用OnJumpPerformed方法
        inputActions.Player.Jump.performed += OnJumpPerformed;
        inputActions.Player.Dash.performed += OnDashPerformed;
        //订阅攻击事件，当玩家按下攻击键时调用OnAttackPerformed方法
        inputActions.Player.Attack.performed += OnAttackPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();

        //取消订阅跳跃事件，防止内存泄漏
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        inputActions.Player.Dash.performed -= OnDashPerformed;
        //取消订阅攻击事件，防止内存泄漏
        inputActions.Player.Attack.performed -= OnAttackPerformed;
    }

    void Update()
    {
        // 禁用输入时强制为零，避免残留输入驱动角色
        MoveInput = IsInputEnabled ? inputActions.Player.Move.ReadValue<Vector2>() : Vector2.zero;

        CheckGroundStatus();
        CheckWallStatus();
        StateMachine.Update();
        // 每帧将物理与逻辑事实同步到 Animator 参数（由 PlayerAnimationDriver 统一写入）
        _animDriver.SyncFrame(HorizontalSpeed, VerticalSpeed, IsGround, IsWall, WallDownSpeed, IsDead);
    }

    private void FixedUpdate()
    {
        StateMachine.FixedUpdate();
    }

    /// <summary>
    /// 检测玩家是否站在地面上，结果写入 IsGround 属性。
    /// </summary>
    private void CheckGroundStatus()
    {
        IsGround = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);
    }

    /// <summary>
    /// 检测玩家是否贴墙，结果写入 IsWall 属性。
    /// 根据 localScale.x 符号动态计算检测盒中心，无需子物体。
    /// </summary>
    private void CheckWallStatus()
    {
        // Knight 默认朝左：scale.x = 1 就是左，= -1 就是右，取反后与运动方向对齐
        float facing = -Mathf.Sign(transform.localScale.x);
        Vector2 origin = (Vector2)transform.position + new Vector2(facing * wallCheckOffset, 0f);
        IsWall = Physics2D.OverlapBox(origin, wallCheckSize, 0f, wallLayer);
    }

    /// <summary>
    /// 处理跳跃输入：验证可跳状态后消耗跳跃次数，触发动画 Trigger 并切换逻辑状态。
    /// </summary>
    /// <param name="context">输入系统回调参数</param>
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (IsDead) return;

        PlayerStateType currentType = StateMachine.CurrentStateType;
        PlayerJumpState jumpState = StateMachine.GetState<PlayerJumpState>();
        if (jumpState == null) return;

        bool accepted = false;
        PlayerJumpKind jumpKind = PlayerJumpKind.None;

        if (currentType == PlayerStateType.WallSlide)
        {
            accepted = jumpState.TryConsumeWallJump(out jumpKind);
        }
        else
        {
            bool inJumpableState = currentType == PlayerStateType.Movement ||
                                   currentType == PlayerStateType.Jump ||
                                   currentType == PlayerStateType.Fall;
            if (!inJumpableState) return;

            accepted = jumpState.TryConsumeStandardJump(IsGround, out jumpKind);
        }

        if (!accepted)
        {
            return;
        }

        _pendingJumpKind = jumpKind;
        if (!StateMachine.TryTransitionTo(PlayerStateType.Jump))
        {
            _pendingJumpKind = PlayerJumpKind.None;
            return;
        }

        TriggerJumpAnimation(jumpKind);
        if (EnableStateDebugLogs)
        {
            Debug.Log($"[PlayerController] 接受跳跃: kind={jumpKind}, ground={IsGround}, wall={IsWall}, remaining={jumpState.RemainingJumps}", this);
        }

        OnJump?.Invoke();
    }

    /// <summary>
    /// 处理冲刺输入：仅在地面移动相位允许进入 Dash 状态。
    /// </summary>
    /// <param name="context">输入系统回调参数</param>
    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (IsDead) return;
        if (StateMachine.CurrentStateType != PlayerStateType.Movement) return;
        if (Time.time < _nextDashReadyTime) return;

        if (!StateMachine.TryTransitionTo(PlayerStateType.Dash))
        {
            return;
        }

        _nextDashReadyTime = Time.time + playerData.dashCooldown;
        _animDriver.TriggerDash();

        if (EnableStateDebugLogs)
        {
            Debug.Log($"[PlayerController] 接受冲刺: nextReady={_nextDashReadyTime:F2}", this);
        }
    }

    /// <summary>
    /// 处理攻击输入：将 Slash 纳入代码 Action 状态，而不是直接裸触发 Animator。
    /// </summary>
    /// <param name="context">输入系统回调参数</param>
    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        PlayerStateType currentType = StateMachine.CurrentStateType;
        bool canStartAction = currentType == PlayerStateType.Movement ||
                              currentType == PlayerStateType.Jump ||
                              currentType == PlayerStateType.Fall ||
                              currentType == PlayerStateType.WallSlide;
        if (!canStartAction) return;

        _pendingActionKind = PlayerActionKind.Slash;
        if (!StateMachine.TryTransitionTo(PlayerStateType.Action))
        {
            _pendingActionKind = PlayerActionKind.None;
            return;
        }

        TriggerActionAnimation(PlayerActionKind.Slash);
        OnAttack?.Invoke();

        if (EnableStateDebugLogs)
        {
            Debug.Log("[PlayerController] 接受动作: Slash", this);
        }
    }

    /// <summary>
    /// 计算并应用伤害，根据剩余血量切换受击或死亡状态。
    /// </summary>
    /// <param name="rawDamage">原始伤害值</param>
    public void TakeDamage(float rawDamage)
    {
        if (IsDead) return;

        float finalDamage = Mathf.Max(rawDamage - playerData.defence, 0);
        health.UpdateHealth(finalDamage);

        if (health.currentHealth > 0)
        {
            OnHit?.Invoke();
            _pendingJumpKind = PlayerJumpKind.None;
            _pendingActionKind = PlayerActionKind.None;
            // 先触发 Hurt 动画再切状态，避免 Trigger 被 SyncFrame 的下一帧覆盖
            _animDriver.TriggerHurt();
            StateMachine.TransitionTo(PlayerStateType.Hurt);
        }
        else
        {
            _pendingJumpKind = PlayerJumpKind.None;
            _pendingActionKind = PlayerActionKind.None;
            StateMachine.TransitionTo(PlayerStateType.Dead);
            // PlayerController.enabled=false 后 Update 停止，手动同步确保 IsDead 立即写入 Animator
            _animDriver.SyncFrame(HorizontalSpeed, VerticalSpeed, IsGround, IsWall, WallDownSpeed, true);
        }
    }

    /// <summary>
    /// 消费挂起的跳跃上下文，供 JumpState.Enter 使用。
    /// </summary>
    public PlayerJumpKind ConsumePendingJumpKind()
    {
        PlayerJumpKind jumpKind = _pendingJumpKind;
        _pendingJumpKind = PlayerJumpKind.None;
        return jumpKind;
    }

    /// <summary>
    /// 消费挂起的动作上下文，供 ActionState.Enter 使用。
    /// </summary>
    public PlayerActionKind ConsumePendingActionKind()
    {
        PlayerActionKind actionKind = _pendingActionKind;
        _pendingActionKind = PlayerActionKind.None;
        return actionKind;
    }

    /// <summary>
    /// 根据跳跃类型触发 Animator 的 Jump / DoubleJump Trigger。
    /// </summary>
    public void TriggerJumpAnimation(PlayerJumpKind jumpKind)
    {
        _animDriver.TriggerJump(jumpKind == PlayerJumpKind.Double);
    }

    /// <summary>
    /// 根据动作类型触发对应 Animator Trigger。
    /// </summary>
    public void TriggerActionAnimation(PlayerActionKind actionKind)
    {
        if (actionKind == PlayerActionKind.Slash)
        {
            _animDriver.TriggerSlash();
        }
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

}
