using UnityEngine;

/// <summary>
/// 巡逻状态
/// </summary>
public class PatrolState : EnemyState
{
    private Transform currentPatrolPoint;
    private bool movingToPoint1 = true;
    private float waitTimer = 0f;

    public PatrolState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        // 初始化巡逻点
        UpdateCurrentPatrolPoint();

        // 播放巡逻动画
        if (enemy.Animator != null)
        {
            enemy.Animator.SetBool("IsPatrolling", true);
            enemy.Animator.SetBool("IsChasing", false);
            enemy.Animator.SetBool("IsAttacking", false);
        }
    }

    public override void Update()
    {
        if (currentPatrolPoint == null) return;

        // 检查是否到达巡逻点
        if (Vector2.Distance(enemy.transform.position, currentPatrolPoint.position) < 0.1f)
        {
            HandleWaitAtPatrolPoint();
        }
        else
        {
            MoveToPatrolPoint();
        }

        // 检查是否应该切换到追逐状态
        CheckTransitionToChase();
    }

    /// <summary>
    /// 更新当前巡逻点
    /// </summary>
    private void UpdateCurrentPatrolPoint()
    {
        currentPatrolPoint = movingToPoint1 ? enemy.PatrolPoint1 : enemy.PatrolPoint2;
    }

    /// <summary>
    /// 在巡逻点等待
    /// </summary>
    private void HandleWaitAtPatrolPoint()
    {
        waitTimer += Time.deltaTime;
        if (waitTimer >= enemy.PatrolWaitTime)
        {
            movingToPoint1 = !movingToPoint1;
            UpdateCurrentPatrolPoint();
            waitTimer = 0f;
        }
    }

    /// <summary>
    /// 移动到巡逻点
    /// </summary>
    private void MoveToPatrolPoint()
    {
        waitTimer = 0f;
        Vector2 direction = (currentPatrolPoint.position - enemy.transform.position).normalized;
        rb.velocity = new Vector2(direction.x * enemy.PatrolSpeed, rb.velocity.y);
        FlipTowardsDirection(direction.x);
    }

    /// <summary>
    /// 检查是否应该切换到追逐状态
    /// </summary>
    private void CheckTransitionToChase()
    {
        float distanceToPlayer = GetDistanceToPlayer();
        if (distanceToPlayer <= enemy.ChaseRange)
        {
            enemy.StateMachine.ChangeState(new ChaseState(enemy));
        }
    }
}