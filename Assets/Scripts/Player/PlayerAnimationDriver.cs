using UnityEngine;

/// <summary>
/// 玩家动画驱动层，是 Animator 参数的唯一写入口。
/// 连续参数由 PlayerController 每帧调用 SyncFrame 统一更新；
/// 一次性 Trigger 由 PlayerController 在对应逻辑事件确认后调用。
/// </summary>
[RequireComponent(typeof(Animator))]
public class PlayerAnimationDriver : MonoBehaviour
{
    private Animator _animator;

    // 连续参数哈希（bool / float，每帧同步）
    private static readonly int HorizontalSpeedHash = Animator.StringToHash("HorizontalSpeed");
    private static readonly int VerticalSpeedHash = Animator.StringToHash("VerticalSpeed");
    private static readonly int IsGroundHash = Animator.StringToHash("IsGround");
    private static readonly int IsTouchWallHash = Animator.StringToHash("IsTouchWall");
    private static readonly int WallDownSpeedHash = Animator.StringToHash("WallDownSpeed");
    private static readonly int IsDeadHash = Animator.StringToHash("IsDead");

    // 一次性 Trigger 参数哈希（每次逻辑事件只触发一次）
    private static readonly int JumpTriggerHash = Animator.StringToHash("Jump");
    private static readonly int DoubleJumpTriggerHash = Animator.StringToHash("DoubleJump");
    private static readonly int DashTriggerHash = Animator.StringToHash("Dash");
    private static readonly int SlashTriggerHash = Animator.StringToHash("Slash");
    private static readonly int HurtTriggerHash = Animator.StringToHash("Hurt");

    /// <summary>
    /// 缓存 Animator 组件。
    /// </summary>
    private void Awake()
    {
        _animator = GetComponent<Animator>();
    }

    /// <summary>
    /// 每帧由 PlayerController 调用，将玩家物理与逻辑状态同步到 Animator 连续参数。
    /// </summary>
    /// <param name="horizontalSpeed">水平速度绝对值</param>
    /// <param name="verticalSpeed">竖直速度（正上负下）</param>
    /// <param name="isGround">是否站在地面</param>
    /// <param name="isTouchWall">是否贴墙</param>
    /// <param name="wallDownSpeed">贴墙下滑速度绝对值（非下滑时为 0）</param>
    /// <param name="isDead">是否已死亡</param>
    public void SyncFrame(float horizontalSpeed, float verticalSpeed,
                          bool isGround, bool isTouchWall,
                          float wallDownSpeed, bool isDead)
    {
        _animator.SetFloat(HorizontalSpeedHash, horizontalSpeed);
        _animator.SetFloat(VerticalSpeedHash, verticalSpeed);
        _animator.SetBool(IsGroundHash, isGround);
        _animator.SetBool(IsTouchWallHash, isTouchWall);
        _animator.SetFloat(WallDownSpeedHash, wallDownSpeed);
        _animator.SetBool(IsDeadHash, isDead);
    }

    /// <summary>
    /// 触发跳跃动画，根据是否为二段跳选择对应的 Trigger。
    /// </summary>
    /// <param name="isDoubleJump">true 时触发 DoubleJump Trigger，否则触发 Jump Trigger</param>
    public void TriggerJump(bool isDoubleJump)
    {
        // 先清掉同类 Trigger，避免上一帧残留导致 Animator 误消费。
        _animator.ResetTrigger(JumpTriggerHash);
        _animator.ResetTrigger(DoubleJumpTriggerHash);
        _animator.SetTrigger(isDoubleJump ? DoubleJumpTriggerHash : JumpTriggerHash);
    }

    /// <summary>
    /// 触发冲刺（Dash）Trigger。
    /// </summary>
    public void TriggerDash()
    {
        _animator.ResetTrigger(DashTriggerHash);
        _animator.SetTrigger(DashTriggerHash);
    }

    /// <summary>
    /// 触发攻击（Slash）Trigger，动画过渡由 Knight.controller 连线管理。
    /// </summary>
    public void TriggerSlash()
    {
        _animator.ResetTrigger(SlashTriggerHash);
        _animator.SetTrigger(SlashTriggerHash);
    }

    /// <summary>
    /// 触发受击（Hurt）Trigger。
    /// </summary>
    public void TriggerHurt()
    {
        _animator.ResetTrigger(HurtTriggerHash);
        _animator.SetTrigger(HurtTriggerHash);
    }
}
