using UnityEngine;

/// <summary>
/// 敌人状态基类。组件引用由 EnemyAI 在 Awake 时缓存后传入，不在构造函数里 GetComponent/Find。
/// </summary>
public abstract class EnemyState : IEnemyState
{
    protected EnemyAI enemy;
    protected Rigidbody2D rb;
    protected Transform player;
    protected Animator anim;

    // 动画状态 hash（完整路径，对应 Base Layer 下的状态名）
    protected static readonly int PatrolHash = Animator.StringToHash("Base Layer.Patrol");
    protected static readonly int ChaseHash = Animator.StringToHash("Base Layer.Chase");
    protected static readonly int AttackHash = Animator.StringToHash("Base Layer.Attack");
    protected static readonly int HurtHash = Animator.StringToHash("Base Layer.Hurt");
    protected static readonly int DeadHash = Animator.StringToHash("Base Layer.Dead");

    public abstract EnemyStateType StateType { get; }

    public EnemyState(EnemyAI enemy)
    {
        this.enemy = enemy;
        this.rb = enemy.Rb;
        this.player = enemy.PlayerTransform;
        this.anim = enemy.Anim;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }

    /// <summary>
    /// 默认允许所有转换，子类重写以实现白名单。
    /// </summary>
    public virtual bool CanTransitionTo(EnemyStateType targetState) => true;

    // -------------------------------------------------------
    // 工具方法
    // -------------------------------------------------------

    /// <summary>
    /// 获取与玩家之间的距离
    /// </summary>
    /// <returns>返回该对象与玩家之间的距离</returns>
    protected float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector2.Distance(enemy.transform.position, player.position);
    }

    /// <summary>
    /// 获取与玩家之间的方向
    /// </summary>
    /// <returns>与玩家之间的方向</returns>
    protected Vector2 GetDirectionToPlayer()
    {
        if (player == null) return Vector2.zero;
        return (player.position - enemy.transform.position).normalized;
    }

    /// <summary>
    /// 改变朝向
    /// </summary>
    /// <param name="dirX">方向</param>
    protected void FlipTowardsDirection(float dirX)
    {
        if (dirX > 0) enemy.transform.localScale = new Vector3(1, 1, 1);
        else if (dirX < 0) enemy.transform.localScale = new Vector3(-1, 1, 1);
    }
}
