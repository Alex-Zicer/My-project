using UnityEngine;

/// <summary>
/// 受击硬直状态
/// </summary>
public class HurtState : EnemyState
{
    private float hurtDuration;
    private float timer;
    private IEnemyState previousState;

    public HurtState(EnemyAI enemy, float duration, IEnemyState previousState) : base(enemy)
    {
        this.hurtDuration = duration;
        this.previousState = previousState;
    }

    public override void Enter()
    {
        // 停止移动
        if (rb != null)
        {
            rb.velocity = Vector2.zero;
        }

        // 重置计时器
        timer = 0f;

        // 播放受击动画
        if (enemy.Animator != null)
        {
            enemy.Animator.SetTrigger("Hurt");
            enemy.Animator.SetBool("IsPatrolling", false);
            enemy.Animator.SetBool("IsChasing", false);
            enemy.Animator.SetBool("IsAttacking", false);
        }
    }

    public override void Update()
    {
        // 更新计时器
        timer += Time.deltaTime;

        // 检查硬直时间是否结束
        if (timer >= hurtDuration)
        {
            ExitHurtState();
        }
    }

    /// <summary>
    /// 退出硬直状态
    /// </summary>
    private void ExitHurtState()
    {
        // 如果有之前的状态，返回之前的状态
        if (previousState != null)
        {
            enemy.StateMachine.ChangeState(previousState);
        }
        else
        {
            // 默认返回巡逻状态
            enemy.StateMachine.ChangeState(new PatrolState(enemy));
        }
    }
}