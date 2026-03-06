using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class PlayerController : MonoBehaviour, IBaseEntity, ICharacterController, IDamageable
{
    public PlayerData playerData;
    private Health health;

    [Header("跳跃设置")]
    [Tooltip("玩家跳跃的力度")]
    public float jumpForce = 12f;
    [Tooltip("地面层，用于检测玩家是否站在地面上")]
    public LayerMask groundLayer;
    [Tooltip("放置一个空物体，地面检测点")]
    public Transform groundCheck;

    //用于ICharacterController接口的事件，其他系统可以订阅这些事件来响应玩家的跳跃和着陆行为
    public event Action OnJump;
    public event Action OnLand;
    public event Action OnAttack;

    #region 接口属性的实现
    //用于IBaseEntity接口的属性
    public float HorizontalSpeed => Mathf.Abs(rb.velocity.x);
    public float VerticalSpeed => rb.velocity.y;

    public float FacingDirection => moveInput.x; //面朝的方向，小于0表示向左，大于0表示向右，根据玩家输入的水平移动方向来确定

    public bool IsDead => false; //玩家是否死亡
    public bool IsGrounded { get; private set; } //玩家是否在地面上,内部自动更新

    private bool lastGrounded; //用于判断落地那一瞬间

    #endregion

    private Rigidbody2D rb;
    private PlayerControls inputActions;//玩家输入系统的引用，使用Unity的新输入系统来处理玩家的输入
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerControls();

        rb.freezeRotation = true; // 冻结旋转，确保玩家不会因为物理碰撞而旋转

        health = GetComponent<Health>();
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
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();

        CheckGroundStatus();
    }

    private void FixedUpdate()
    {
        //计算目标水平速度
        float targetXVelocity = moveInput.x * playerData.moveSpeed;

        //使用MoveTowards使速度更丝滑，不会出现瞬间变向
        float currentX = rb.velocity.x;
        float newX = Mathf.MoveTowards(currentX, targetXVelocity, playerData.moveSpeedMultiplier * Time.fixedDeltaTime);

        //应用速度，保持垂直速度不变
        rb.velocity = new Vector2(newX, rb.velocity.y);
    }

    [Header("地面检测设置")]
    [Tooltip("地面检测的范围大小")]
    public Vector2 groundCheckSize = new Vector2(0.5f, 0.1f);
    [Tooltip("相对于中心点的偏移，确保检测点位于玩家脚下")]
    public float groundCheckOffset = 0.1f;

    /// <summary>
    /// 检测人物是否站在地面上
    /// </summary>
    private void CheckGroundStatus()
    {
        Vector2 checkPosition = (Vector2)transform.position + new Vector2(0, groundCheckOffset);

        //使用OverlapBox检测玩家是否在地面上，groundCheck是一个空物体，放置在玩家脚下，检测范围为0.2f，检测的层为groundLayer
        IsGrounded = Physics2D.OverlapBox(groundCheck.position, groundCheckSize, 0f, groundLayer);

        if(IsGrounded && !lastGrounded)
        {
            OnLand?.Invoke(); //触发着陆事件
        }
    }

    /// <summary>
    /// 实现玩家跳跃功能的方法，当玩家按下跳跃键时被调用。它通过设置刚体的垂直速度来实现跳跃效果。
    /// </summary>
    /// <param name="context">用来实现事件系统的参数</param>
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);
        OnJump?.Invoke(); //触发跳跃事件
        Debug.Log("跳跃！");
    }

    private void OnAttackPerformed(InputAction.CallbackContext context)
    {
        OnAttack?.Invoke();
        Debug.Log("攻击！");
    }

    /// <summary>
    /// 计算受到的伤害
    /// </summary>
    /// <param name="rawDamage">受到的原始伤害</param>
    public void TakeDamage(float rawDamage)
    {
        float finalDamage = Mathf.Max(rawDamage - playerData.defence, 0);
        health.UpdateHealth(finalDamage);
    }
}
