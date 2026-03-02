using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerController : MonoBehaviour
{
    [Header("移动设置")]
    [Tooltip("玩家的移动速度，单位为每秒的单位距离。")]
    public float moveSpeed = 8f;
    //玩家的加速度，起步时的加速度，以便人物看起来更丝滑
    public float acceleration = 50f;

    [Header("跳跃设置")]
    [Tooltip("玩家跳跃的力度")]
    public float jumpForce = 12f;

    private Rigidbody2D rb;
    private PlayerControls inputActions;
    private Vector2 moveInput;

    private void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        inputActions = new PlayerControls();

        rb.freezeRotation = true; // 冻结旋转，确保玩家不会因为物理碰撞而旋转

    }

    private void OnEnable()
    {
        inputActions.Player.Enable();

        //订阅跳跃事件，当玩家按下跳跃键时调用OnJumpPerformed方法
        inputActions.Player.Jump.performed += OnJumpPerformed;
    }

    private void OnDisable()
    {
        inputActions.Player.Disable();

        //取消订阅跳跃事件，防止内存泄漏
        inputActions.Player.Jump.performed -= OnJumpPerformed;
    }

    // Update is called once per frame
    void Update()
    {
        // 获取玩家的输入，更新移动向量
        moveInput = inputActions.Player.Move.ReadValue<Vector2>();
    }

    private void FixedUpdate()
    {
        //计算目标水平速度
        float targetXVelocity = moveInput.x * moveSpeed;

        //使用MoveTowards使速度更丝滑，不会出现瞬间变向
        float currentX = rb.velocity.x;
        float newX = Mathf.MoveTowards(currentX, targetXVelocity, acceleration * Time.fixedDeltaTime);

        //应用速度，保持垂直速度不变
        rb.velocity = new Vector2(newX, rb.velocity.y);
    }

    /// <summary>
    /// 实现玩家跳跃功能的方法，当玩家按下跳跃键时被调用。它通过设置刚体的垂直速度来实现跳跃效果。
    /// </summary>
    /// <param name="context">用来实现事件系统的参数</param>
    private void OnJumpPerformed(InputAction.CallbackContext context)
    {
        rb.velocity = new Vector2(rb.velocity.x, jumpForce);

        Debug.Log("跳跃！");
    }
}
