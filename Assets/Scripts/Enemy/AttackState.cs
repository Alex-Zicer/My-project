using UnityEngine;

/// <summary>
/// ¹¥»÷×´Ì¬
/// </summary>
public class AttackState : EnemyState
{
    private float nextAttackTime = 0f;

    public AttackState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        // ½øÈë¹¥»÷×´Ì¬Ê±Í£Ö¹ÒÆ¶¯
        if (rb != null)
        {
            rb.velocity = new Vector2(0, rb.velocity.y);
        }

        // ²¥·Å¹¥»÷¶¯»­
        if (enemy.Animator != null)
        {
            enemy.Animator.SetBool("IsPatrolling", false);
            enemy.Animator.SetBool("IsChasing", false);
            enemy.Animator.SetBool("IsAttacking", true);
        }
    }

    public override void Update()
    {
        if (player == null)
        {
            enemy.StateMachine.ChangeState(new PatrolState(enemy));
            return;
        }

        // ÃæÏòÍæ¼Ò
        FacePlayer();

        // Ö´ÐÐ¹¥»÷
        HandleAttack();

        // ¼ì²é×´Ì¬×ª»»
        CheckTransitions();
    }

    /// <summary>
    /// ÃæÏòÍæ¼Ò
    /// </summary>
    private void FacePlayer()
    {
        Vector2 direction = GetDirectionToPlayer();
        FlipTowardsDirection(direction.x);
    }

    /// <summary>
    /// ´¦Àí¹¥»÷Âß¼­
    /// </summary>
    private void HandleAttack()
    {
        if (Time.time >= nextAttackTime)
        {
            PerformAttack();
            nextAttackTime = Time.time + 1f / enemy.AttackRate;
        }
    }

    /// <summary>
    /// Ö´ÐÐ¹¥»÷
    /// </summary>
    private void PerformAttack()
    {
        if (player == null) return;

        // ´¥·¢¹¥»÷¶¯»­
        if (enemy.Animator != null)
        {
            enemy.Animator.SetTrigger("Attack");
        }

        IDamageable playerDamageable = player.GetComponent<IDamageable>();
        if (playerDamageable != null)
        {
            playerDamageable.TakeDamage(enemy.AttackDamage, enemy.AttackImpactForce);
            Debug.Log("µÐÈË¹¥»÷Íæ¼Ò");
        }
    }

    /// <summary>
    /// ¼ì²é×´Ì¬×ª»»
    /// </summary>
    private void CheckTransitions()
    {
        float distanceToPlayer = GetDistanceToPlayer();

        // Èç¹û³¬³ö¹¥»÷·¶Î§£¬ÇÐ»»»Ø×·Öð×´Ì¬
        if (distanceToPlayer > enemy.AttackRange)
        {
            enemy.StateMachine.ChangeState(new ChaseState(enemy));
        }
    }
}