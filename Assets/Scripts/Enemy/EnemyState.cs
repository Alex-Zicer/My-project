using UnityEngine;

/// <summary>
/// 敌人状态基类
/// </summary>
public abstract class EnemyState : IEnemyState
{
    protected EnemyAI enemy;
    protected Rigidbody2D rb;
    protected Transform player;

    public EnemyState(EnemyAI enemy)
    {
        this.enemy = enemy;
        this.rb = enemy.GetComponent<Rigidbody2D>();
        this.player = GameObject.FindGameObjectWithTag("Player")?.transform;
    }

    public virtual void Enter() { }
    public virtual void Exit() { }
    public virtual void Update() { }
    public virtual void FixedUpdate() { }

    /// <summary>
    /// 获取到玩家的距离
    /// </summary>
    protected float GetDistanceToPlayer()
    {
        if (player == null) return float.MaxValue;
        return Vector2.Distance(enemy.transform.position, player.position);
    }

    /// <summary>
    /// 获取到玩家的方向
    /// </summary>
    protected Vector2 GetDirectionToPlayer()
    {
        if (player == null) return Vector2.zero;
        return (player.position - enemy.transform.position).normalized;
    }

    /// <summary>
    /// 转向
    /// </summary>
    protected void FlipTowardsDirection(float direction)
    {
        if (direction > 0)
        {
            enemy.transform.localScale = new Vector3(1, 1, 1);
        }
        else if (direction < 0)
        {
            enemy.transform.localScale = new Vector3(-1, 1, 1);
        }
    }
}