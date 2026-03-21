using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;
using System.Data;
using UnityEngine.Experimental.Rendering;
using Unity.VisualScripting;

public class PlayerController : MonoBehaviour, IDamageable, ICharacterController
{
    [Header("数据引用")]
    [SerializeField] private PlayerData playerData;
    [SerializeField] private WeaponData defaultWeapon;

    [Header("组件引用")]
    [SerializeField] private Health health;
    [SerializeField] private Animator anim;
    private PlayerControls inputActions;//玩家输入系统的引用，使用Unity的新输入系统来处理玩家的输入
    private LayerMask enemyLayer;

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
    private int currentJumpCount;

    #region 接口事件
    //用于ICharacterController接口的事件，其他系统可以订阅这些事件来响应玩家的跳跃和着陆行为
    public event Action OnJump;
    public event Action OnAttack;

    #endregion

    #region 公开属性状态类访问

    public Vector2 MoveInput { get; private set; }
    public PlayerStateMachine StateMachine { get; private set; }
    public Rigidbody2D Rb { get; private set; }
    public PlayerData PlayerData => playerData;
    public Animator animator { get; private set; }
    public WeaponData CurrentWeapon { get; private set; }
    public LayerMask EnemyLayer => enemyLayer;

    #endregion

    public float HorizontalSpeed => Mathf.Abs(Rb.velocity.x);
    public float VerticalSpeed => Rb.velocity.y;
    public bool IsGround { get; private set; }
    public bool IsDead => StateMachine.CurrentStateType == PlayerStateType.Dead;



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
        // 获取玩家的输入，更新移动向量
        MoveInput = inputActions.Player.Move.ReadValue<Vector2>();

        CheckGroundStatus();
        StateMachine.Update();

        Debug.Log($"代码状态机当前状态: {StateMachine.CurrentStateType}");

        if (Input.GetKeyDown(KeyCode.E))
        {
            TakeDamage(10);
        }
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
        if (IsGround)//从地上开始跳能跳两次
        {
            currentJumpCount = 0;
        }
        else if (!IsGround && currentJumpCount < canJumpCount)//在空中只能跳一次
        {
            currentJumpCount = 1;
        }

        //如果目前是以下状态并且还没有进行过二段跳就能够进行跳跃
        PlayerStateType currentType = StateMachine.CurrentStateType;
        bool canJump = (currentType == PlayerStateType.Movement ||
                        currentType == PlayerStateType.Jump || currentType == PlayerStateType.Fall) && currentJumpCount < canJumpCount;
        if (canJump)
        {
            currentJumpCount++;
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
            (StateMachine.CurrentState as PlayerAttackState)?.QueueNextAttack();
        }
        else if (IsGround && current != PlayerStateType.Hurt && current != PlayerStateType.Dead)
        {
            StateMachine.TransitionTo(PlayerStateType.Attack);//进行第一段攻击
        }
        OnAttack?.Invoke();
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

}
