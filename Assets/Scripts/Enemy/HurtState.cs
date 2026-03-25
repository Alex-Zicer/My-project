using UnityEngine;

public class HurtState : EnemyState
{
    public override EnemyStateType StateType => EnemyStateType.Hurt;

    private float _timer;
    // 受伤前的状态类型，恢复时切回对应状态
    private EnemyStateType _previousStateType;

    public HurtState(EnemyAI enemy) : base(enemy) { }

    /// <summary>
    /// EnemyAI.TakeDamage 调用前先设置前驱状态，再 TransitionTo(Hurt)。
    /// 先记录受击之前敌人的状态，方便受击状态结束之后切换回去
    /// </summary>
    public void SetPreviousState(EnemyStateType previous)
        => _previousStateType = previous;

    /// <summary>
    /// 受击时将速度置为0
    /// </summary>
    public override void Enter()
    {
        _timer = 0f;
        //受到攻击会被击退
        Vector2 knockbackDir = (enemy.transform.position - player.transform.position).normalized;
        rb.velocity = knockbackDir * enemy.KnockbackForce;
        if (anim != null) anim.CrossFade(HurtHash, 0.05f);
    }

    /// <summary>
    /// 计时受击过程，当受击时间结束之后切换回原本的状态
    /// </summary>
    public override void Update()
    {
        _timer += Time.deltaTime;
        if (_timer >= enemy.HurtDuration)
            Recover();
    }

    // 硬直中只能被死亡打断
    public override bool CanTransitionTo(EnemyStateType targetState)
    {
        if (_timer >= enemy.HurtDuration) return true;
        return targetState == EnemyStateType.Dead;
    }

    /// <summary>
    /// 切换回受击之前的状态
    /// </summary>
    private void Recover()
    {
        EnemyStateType recoverTo = _previousStateType == EnemyStateType.Patrol
            ? EnemyStateType.Patrol
            : EnemyStateType.Chase;
        enemy.StateMachine.TransitionTo(recoverTo);
    }
}
