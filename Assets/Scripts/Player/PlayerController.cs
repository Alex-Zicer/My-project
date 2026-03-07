using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerController : MonoBehaviour, IBaseEntity, ICharacterController, IDamageable
{
    public PlayerData playerData;
    private Health health;
    private CameraController cameraController;

    [Header("跳跃设置")]
    [Tooltip("玩家跳跃力量")]
    public float jumpForce = 12f;
    [Tooltip("地面层，用于检测玩家是否站立在地面上")]
    public LayerMask groundLayer;
    [Tooltip("地面检测点，用于检测")]
    public Transform groundCheck;

    [Header("攻击设置")]
    [Tooltip("攻击检测点")]
    public Transform attackPoint;
    [Tooltip("攻击范围大小")]
    public Vector2 attackRange = new Vector2(1f, 1f);
    [Tooltip("敌人层")]
    public LayerMask enemyLayer;
    [Tooltip("攻击力")]
    public float attackDamage = 20f;
    [Tooltip("攻击速度")]
    public float attackRate = 0.5f;
    private float nextAttackTime = 0f;

    [Header("技能系统")]
    [Tooltip("冲刺速度")]
    public float dashSpeed = 20f;
    [Tooltip("冲刺持续时间")]
    public float dashDuration = 0.2f;
    [Tooltip("冲刺冷却时间")]
    public float dashCooldown = 1f;
    private bool isDashing = false;
    private float nextDashTime = 0f;

    //实现ICharacterController接口的事件，输入系统会自动处理这些事件对应的玩家输入，如跳跃、落地等
    public event Action OnJump;
    public event Action OnLand;
    public event Action OnAttack;
    public event Action OnDash;

    #region 接口属性的实现
    //实现IBaseEntity接口的属性
    public float HorizontalSpeed => Mathf.Abs(rb.velocity.x);
    public float VerticalSpeed => rb.velocity.y;

    public float FacingDirection => moveInput.x; //角色的方向，小于0表示向左，大于0表示向右，根据玩家水平移动输入来确定

    public bool IsDead => false; //角色是否死亡
    public bool IsGrounded { get; private set; } //角色是否在地面上，内部可读写

    private bool lastGrounded; //用于判断上一帧状态

    #endregion

    private Rigidbody2D rb;
    private PlayerControls inputActions;//输入系统的引用，使用Unity新输入系统来处理玩家输入

    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerControls();

        rb.freezeRotation = true; // 冻结旋转，确保角色不会因为物理碰撞而旋转

        //初始化角色血量
        health = GetComponent<Health>();
        health.maxHealth = playerData.maxHealth;
        health.currentHealth = health.maxHealth;

        // 初始化相机控制器
        cameraController = FindObjectOfType<CameraController>();
    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        //注册跳跃事件，当玩家跳跃时调用OnJumpPerformed方法
        inputActions.Player.Jump.performed += OnJumpPerformed;
        //注册攻击事件，当玩家攻击时调用OnAttackPerformed方法
        inputActions.Player.Attack.performed += OnAttackPerformed;
        //注册技能事件，当玩家使用技能时调用OnDashPerformed方法
        inputActions.Player.Dash.performed += OnDashPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();

        //取消注册跳跃事件，防止内存泄漏
        inputActions.Player.Jump.performed -= OnJumpPerformed;
        //取消注册攻击事件，防止内存泄漏
        inputActions.Player.Attack.performed -= OnAttackPerformed;
        //取消注册技能事件，防止内存泄漏
        inputActions.Player.Dash.performed -= OnDashPerformed;
    }

    // Update is called once per frame
    void Update()
    {
        HandleInput();
        CheckGroundStatus();
    }

    private void FixedUpdate()
    {
        if (!isDashing)
        {
            HandleMovement();
        }
    }

    /// <summary>
    /// 处理玩家输入
    /// </summary>
    private void HandleInput()
    {
        // 获取玩家输入，处理移动方向
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
    }

    /// <summary>
    /// 处理玩家移动
    /// </summary>
    private void HandleMovement()
    {
        // 初始化目标水平速度
        float targetXVelocity = moveInput.x * playerData.moveSpeed;

        //使用MoveTowards使速度平滑变化，实现移动缓冲效果
        float currentX = rb.velocity.x;
        float newX = Mathf.MoveTowards(currentX, targetXVelocity, playerData.moveSpeedMultiplier * Time.fixedDeltaTime);

        //应用速度，保持垂直速度不变
        rb.velocity = new Vector2(newX, rb.velocity.y);
    }

    [Header("地面检测设置")]
    [Tooltip("地面检测范围大小")]
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [Tooltip("地面检测点偏移量，确保检测点位置正确")]
    public float groundCheckOffset = 0.1f;

    /// <summary>
    /// 检测角色是否站立在地面上
    /// </summary>
    private void CheckGroundStatus()
    {
        Vector2 checkPosition = (Vector2)transform.position + new Vector2(0, groundCheckOffset);

        //使用OverlapBox检测是否在地面上，groundCheck是一个检测点，检测范围为groundCheckSize，检测层为groundLayer
        IsGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if (IsGrounded && !lastGrounded)
        {
            OnLand?.Invoke(); //触发落地事件
        }
    }

    /// <summary>
    /// 实现玩家跳跃功能的方法，当玩家跳跃时调用。通过改变角色的垂直速度来实现跳跃效果。
    /// </summary>
    /// <param name="context">输入系统的上下文</param>
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        if (IsGrounded)
        {
            rb.velocity = new Vector2(rb.velocity.x, jumpForce);
            OnJump?.Invoke(); //触发跳跃事件
            Debug.Log("跳跃了");
        }
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        if (Time.time >= nextAttackTime)
        {
            Attack();
            nextAttackTime = Time.time + 1f / attackRate;
        }
    }

    private void OnDashPerformed(InputAction.CallbackContext context)
    {
        if (Time.time >= nextDashTime)
        {
            StartCoroutine(Dash());
            nextDashTime = Time.time + dashCooldown;
        }
    }

    private IEnumerator Dash()
    {
        isDashing = true;
        OnDash?.Invoke();
        Debug.Log("执行冲刺");

        // 保存当前重力
        float originalGravity = rb.gravityScale;
        rb.gravityScale = 0f; // 防止冲刺时受重力影响

        // 设置冲刺方向
        float dashDirection = moveInput.x != 0 ? moveInput.x : (transform.localScale.x > 0 ? 1 : -1);
        rb.velocity = new Vector2(dashDirection * dashSpeed, 0f);

        // 等待冲刺持续时间
        yield return new WaitForSeconds(dashDuration);

        // 结束冲刺
        rb.gravityScale = originalGravity;
        isDashing = false;
    }

    private void Attack()
    {
        OnAttack?.Invoke();
        Debug.Log("攻击了");

        // 检测碰撞
        Collider2D[] hitEnemies = Physics2D.OverlapBoxAll(attackPoint.position, attackRange, 0f, enemyLayer);

        // 对每个敌人造成伤害
        foreach (Collider2D enemy in hitEnemies)
        {
            IDamageable damageable = enemy.GetComponent<IDamageable>();
            if (damageable != null)
            {
                damageable.TakeDamage(attackDamage);
                Debug.Log("击中敌人:" + enemy.name);
                // 触发帧冻结
                if (cameraController != null)
                {
                    cameraController.TriggerFreeze();
                }
            }
        }
    }

    /// <summary>
    /// 角色受到伤害
    /// </summary>
    /// <param name="rawDamage">角色受到的原始伤害</param>
    public void TakeDamage(float rawDamage)
    {
        float finalDamage = Mathf.Max(rawDamage - playerData.defence, 0);
        health.UpdateHealth(finalDamage);
        // 触发镜头抖动
        if (cameraController != null)
        {
            cameraController.TriggerShake();
        }
    }

    // 显示攻击范围
    private void OnDrawGizmosSelected()
    {
        if (attackPoint == null)
            return;

        Gizmos.color = Color.red;
        Gizmos.DrawWireCube(attackPoint.position, attackRange);
    }
}