using UnityEngine;

/// <summary>
/// ×·Öð×´Ì¬
/// </summary>
public class ChaseState : EnemyState
{
    public ChaseState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        // ½øÈë×·Öð×´Ì¬Ê±µÄ³õÊ¼»¯

        // ²¥·Å×·Öð¶¯»­
        if (enemy.Animator != null)
        {
            enemy.Animator.SetBool("IsPatrolling", false);
            enemy.Animator.SetBool("IsChasing", true);
            enemy.Animator.SetBool("IsAttacking", false);
        }
    }

    public override void Update()
    {
        if (player == null)
        {
            enemy.StateMachine.ChangeState(new PatrolState(enemy));
            return;
        }

        MoveTowardsPlayer();
        CheckTransitions();
    }

    /// <summary>
    /// ÏòÍæ¼ÒÒÆ¶¯
    /// </summary>
    private void MoveTowardsPlayer()
    {
        Vector2 direction = GetDirectionToPlayer();
        rb.velocity = new Vector2(direction.x * enemy.ChaseSpeed, rb.velocity.y);
        FlipTowardsDirection(direction.x);
    }

    /// <summary>
    /// ¼ì²é×´Ì¬×ª»»
    /// </summary>
    private void CheckTransitions()
    {
        float distanceToPlayer = GetDistanceToPlayer();

        // Èç¹ûÔÚ¹¥»÷·¶Î§ÄÚ£¬ÇÐ»»µ½¹¥»÷×´Ì¬
        if (distanceToPlayer <= enemy.AttackRange)
        {
            enemy.StateMachine.ChangeState(new AttackState(enemy));
        }
        // Èç¹û³¬³ö×·Öð·¶Î§£¬ÇÐ»»»ØÑ²Âß×´Ì¬
        else if (distanceToPlayer > enemy.ChaseRange)
        {
            enemy.StateMachine.ChangeState(new PatrolState(enemy));
        }
    }
}