using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// Button 动画触发器。
/// 挂在 Button 上，负责接收输入并驱动一个或多个动画目标。
/// </summary>
public class ButtonAnimationTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler, ISubmitHandler
{
    /// <summary>
    /// 单个动画目标的配置数据。
    /// </summary>
    [Serializable]
    private class AnimationTarget
    {
        // 需要显示或隐藏的动画对象。
        [SerializeField] private GameObject _targetObject;

        // 负责播放悬停与选中动画的 Animator。
        [SerializeField] private Animator _animator;

        /// <summary>
        /// 获取需要显示或隐藏的动画对象。
        /// </summary>
        public GameObject TargetObject => _targetObject;

        /// <summary>
        /// 获取当前目标使用的 Animator。
        /// </summary>
        public Animator Animator => _animator;

    }

    // 当前按钮需要驱动的全部动画目标。
    [Header("动画目标")]
    [SerializeField] private AnimationTarget[] _targets = Array.Empty<AnimationTarget>();

    // 鼠标悬停时发送给 Animator 的 Bool 参数名称。
    [Header("Animator Parameters")]
    [SerializeField] private string _hoveredBoolName = "IsHovered";

    // 按钮点击时发送给 Animator 的 Trigger 名称。
    [SerializeField] private string _selectedTriggerName = "Selected";

    // 当前物体上的 Selectable，用于判断按钮是否可交互。
    private Selectable _selectable;

    /// <summary>
    /// 缓存 Selectable，并初始化全部动画目标的悬停与显示状态。
    /// </summary>
    private void Awake()
    {
        // 缓存按钮自身的交互状态组件，避免后续重复获取。
        _selectable = GetComponent<Selectable>();
    }

    /// <summary>
    /// 等待 Animator 完成初始化后，再设置动画目标的初始状态。
    /// </summary>
    private void Start()
    {
        // 初始化动画目标的悬停状态，并默认隐藏动画对象。
        InitializeTargets();
    }

    /// <summary>
    /// 鼠标进入时设置悬停状态。
    /// </summary>
    /// <param name="eventData">指针事件数据。</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        // 当按钮不可交互时，不播放任何动画。
        if (!CanPlayAnimation())
        {
            return;
        }

        // 鼠标进入时，显示动画对象并切换为悬停状态。
        SetHoverState(true);
    }

    /// <summary>
    /// 鼠标离开时取消悬停状态并隐藏动画对象。
    /// </summary>
    /// <param name="eventData">指针事件数据。</param>
    public void OnPointerExit(PointerEventData eventData)
    {
        // 鼠标离开时，先通知 Animator 退出悬停状态。
        SetHoverState(false);

        // 当前方案下离开按钮后直接隐藏动画对象。
        HideAllTargets();
    }

    /// <summary>
    /// 鼠标点击时播放选中动画。
    /// </summary>
    /// <param name="eventData">指针事件数据。</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        // 鼠标点击时，复用统一的点击播放逻辑。
        OnButtonClicked();
    }

    /// <summary>
    /// 手柄或键盘提交时播放选中动画。
    /// </summary>
    /// <param name="eventData">事件数据。</param>
    public void OnSubmit(BaseEventData eventData)
    {
        // 键盘或手柄提交时，沿用同一套点击动画逻辑。
        OnButtonClicked();
    }

    /// <summary>
    /// 播放按钮点击动画。
    /// 可供 Inspector 或外部代码手动调用。
    /// </summary>
    public void OnButtonClicked()
    {
        // 当按钮不可交互时，不播放任何动画。
        if (!CanPlayAnimation())
        {
            return;
        }

        // 点击时播放选中动画。
        PlayTrigger(_selectedTriggerName);
    }

    /// <summary>
    /// 判断当前按钮是否允许播放动画。
    /// </summary>
    /// <returns>可播放时返回 true，否则返回 false。</returns>
    private bool CanPlayAnimation()
    {
        return _selectable == null || (_selectable.IsActive() && _selectable.IsInteractable());
    }

    /// <summary>
    /// 初始化全部动画目标的状态。
    /// </summary>
    private void InitializeTargets()
    {
        for (int i = 0; i < _targets.Length; i++)
        {
            AnimationTarget target = _targets[i];

            // 跳过未配置的空目标，避免空引用报错。
            if (target == null)
            {
                continue;
            }

            // 初始化时先清空悬停状态，避免 Animator 落在错误状态。
            SetHoverState(target, false);

            // 初始化时默认隐藏动画对象，等待交互触发。
            SetTargetVisible(target, false);
        }
    }

    /// <summary>
    /// 给全部动画目标设置统一的悬停状态。
    /// </summary>
    /// <param name="isHovered">是否处于悬停状态。</param>
    private void SetHoverState(bool isHovered)
    {
        for (int i = 0; i < _targets.Length; i++)
        {
            AnimationTarget target = _targets[i];

            // 跳过未配置的空目标，避免空引用报错。
            if (target == null)
            {
                continue;
            }

            // 进入悬停时先显示目标对象，确保高亮动画可见。
            if (isHovered)
            {
                SetTargetVisible(target, true);
            }

            // 将悬停状态同步给每个目标的 Animator。
            SetHoverState(target, isHovered);
        }
    }

    /// <summary>
    /// 给单个动画目标设置悬停状态。
    /// </summary>
    /// <param name="target">目标配置。</param>
    /// <param name="isHovered">是否处于悬停状态。</param>
    private void SetHoverState(AnimationTarget target, bool isHovered)
    {
        // 仅在 Animator 和参数名称都有效时才同步悬停状态。
        if (target.Animator != null && !string.IsNullOrEmpty(_hoveredBoolName))
        {
            target.Animator.SetBool(_hoveredBoolName, isHovered);
        }
    }

    /// <summary>
    /// 给全部动画目标发送同一个 Animator Trigger。
    /// </summary>
    /// <param name="triggerName">要发送的 Trigger 名称。</param>
    private void PlayTrigger(string triggerName)
    {
        for (int i = 0; i < _targets.Length; i++)
        {
            AnimationTarget target = _targets[i];

            // 跳过未配置的空目标，避免空引用报错。
            if (target == null)
            {
                continue;
            }

            // 播放动画前先显示目标对象。
            SetTargetVisible(target, true);

            // 仅在 Animator 和 Trigger 名称都有效时才触发动画。
            if (target.Animator != null && !string.IsNullOrEmpty(triggerName))
            {
                target.Animator.SetTrigger(triggerName);
            }
        }
    }

    /// <summary>
    /// 隐藏全部动画目标。
    /// </summary>
    private void HideAllTargets()
    {
        for (int i = 0; i < _targets.Length; i++)
        {
            AnimationTarget target = _targets[i];

            // 跳过未配置的空目标，避免空引用报错。
            if (target == null)
            {
                continue;
            }

            // 鼠标离开后关闭对应的动画对象。
            SetTargetVisible(target, false);
        }
    }

    /// <summary>
    /// 设置指定动画目标的显示状态。
    /// </summary>
    /// <param name="target">目标配置。</param>
    /// <param name="isVisible">是否显示。</param>
    private static void SetTargetVisible(AnimationTarget target, bool isVisible)
    {
        // 仅在目标对象存在时切换显隐。
        if (target.TargetObject != null)
        {
            target.TargetObject.SetActive(isVisible);
        }
    }
}
