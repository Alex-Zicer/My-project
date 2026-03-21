using UnityEngine;

public class PatrolState : EnemyState
{
    public override EnemyStateType StateType => EnemyStateType.Patrol;

    private bool _movingToPoint1 = true;
    private Transform _currentTarget;
    private float _waitTimer;

    public PatrolState(EnemyAI enemy) : base(enemy) { }

    public override void Enter()
    {
        _waitTimer = 0f;
        UpdateTarget();
        if (anim != null) anim.CrossFade(PatrolHash, 0.1f);
    }

    /// <summary>
    /// 如果未设置巡逻地点，直接返回；当到达巡逻边界之一进入等待状态，否则继续向着巡逻目标前进
    /// 如果玩家进入感知范围就切换到追击状态
    /// </summary>
    public override void Update()
    {
        if (_currentTarget == null) return;

        if (Vector2.Distance(enemy.transform.position, _currentTarget.position) < 0.1f)
            HandleWait();
        else
            MoveToTarget();

        // 感知到玩家 → 追击
        if (player != null && GetDistanceToPlayer() <= enemy.ChaseRange)
            enemy.StateMachine.TransitionTo(EnemyStateType.Chase);
    }

    /// <summary>
    /// 退出巡逻状态时，将水平速度设置为0，但是保持垂直速度不变，以免进入下一个状态时还有初始速度
    /// 为了确保敌人在停止水平移动的同时可以进行跳跃或者其他垂直方向的运动
    /// </summary>
    public override void Exit()
    {
        rb.velocity = new Vector2(0, rb.velocity.y);
    }

    // 巡逻状态只允许转换到追击和受伤（死亡在 EnemyAI.TakeDamage 里强制切）
    public override bool CanTransitionTo(EnemyStateType targetState)
        => targetState == EnemyStateType.Chase ||
           targetState == EnemyStateType.Hurt ||
           targetState == EnemyStateType.Dead;

    /// <summary>
    /// 更新当前的巡逻地点
    /// </summary>
    private void UpdateTarget()
        => _currentTarget = _movingToPoint1 ? enemy.PatrolPoint1 : enemy.PatrolPoint2;

    /// <summary>
    /// 到达巡逻目标地点之一后，等待一段时间
    /// </summary>
    private void HandleWait()
    {
        _waitTimer += Time.deltaTime;
        if (_waitTimer >= enemy.PatrolWaitTime)
        {
            _movingToPoint1 = !_movingToPoint1;
            UpdateTarget();
            _waitTimer = 0f;
        }
    }

    /// <summary>
    /// 向着巡逻目标点前进，并且确保巡逻时的动画朝向与移动方向一致
    /// </summary>
    private void MoveToTarget()
    {
        _waitTimer = 0f;
        Vector2 dir = ((Vector2)_currentTarget.position - (Vector2)enemy.transform.position).normalized;
        rb.velocity = new Vector2(dir.x * enemy.PatrolSpeed, rb.velocity.y);
        FlipTowardsDirection(dir.x);
    }
}
