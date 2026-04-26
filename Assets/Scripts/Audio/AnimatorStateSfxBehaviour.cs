using UnityEngine;

/// <summary>
/// Animator 状态音效模式。
/// </summary>
public enum AnimatorStateSfxMode
{
    PlayOnceOnEnter = 0,
    PlayLoopOnEnterStopOnExit = 1
}

/// <summary>
/// 通用 Animator 状态音效行为。
/// 可用于在进入状态时播放一次性音效，或在进入/退出状态时管理循环音效。
/// </summary>
public class AnimatorStateSfxBehaviour : StateMachineBehaviour
{
    [SerializeField] private string eventId;
    [SerializeField] private AnimatorStateSfxMode playbackMode = AnimatorStateSfxMode.PlayOnceOnEnter;

    /// <summary>
    /// 进入状态时按配置播放音效。
    /// </summary>
    public override void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        IActionSfxEmitter emitter = animator.GetComponent<IActionSfxEmitter>();
        if (emitter == null || string.IsNullOrWhiteSpace(eventId))
        {
            return;
        }

        switch (playbackMode)
        {
            case AnimatorStateSfxMode.PlayOnceOnEnter:
                emitter.PlaySfx(eventId);
                break;
            case AnimatorStateSfxMode.PlayLoopOnEnterStopOnExit:
                emitter.PlayLoopSfx(eventId);
                break;
        }
    }

    /// <summary>
    /// 退出状态时停止对应的循环音效。
    /// </summary>
    public override void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        if (playbackMode != AnimatorStateSfxMode.PlayLoopOnEnterStopOnExit)
        {
            return;
        }

        IActionSfxEmitter emitter = animator.GetComponent<IActionSfxEmitter>();
        if (emitter == null)
        {
            return;
        }

        emitter.StopLoopSfx(eventId);
    }
}