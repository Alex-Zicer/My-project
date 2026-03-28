using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;

/// <summary>
/// UI 交互音效发射器：支持默认事件、单控件覆盖与局部静音。
/// </summary>
public class UISoundEmitter : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler
{
    // 当前控件使用的 UI 音效角色。
    [Header("Defaults")]
    [FormerlySerializedAs("soundType")]
    [SerializeField] private UIAudioRole role = UIAudioRole.Default;

    // 点击音效覆盖事件；为空时走默认角色事件。
    [Header("Overrides")]
    [SerializeField] private AudioEventSO clickOverride;

    // 悬停音效覆盖事件；为空时走默认角色事件。
    [SerializeField] private AudioEventSO hoverOverride;

    // 是否禁用当前控件的全部音效。
    [Header("Flags")]
    [SerializeField] private bool muteThisControl;

    // 是否启用点击音效。
    [SerializeField] private bool enableClickSound = true;

    // 是否启用悬停音效。
    [SerializeField] private bool enableHoverSound = true;

    // 当控件为 Toggle 时，是否仅在 On 状态播放点击音效。
    [SerializeField] private bool requireToggleOnForClick = true;

    // 可选的 Toggle 引用，用于点击状态校验。
    private Toggle toggle;

    /// <summary>
    /// 缓存控件上的 Toggle 组件。
    /// </summary>
    private void Awake()
    {
        toggle = GetComponent<Toggle>();
    }

    /// <summary>
    /// 指针点击时播放点击音效。
    /// </summary>
    /// <param name="eventData">指针事件数据。</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (muteThisControl || !enableClickSound)
        {
            return;
        }

        // 对 Toggle 控件执行状态过滤，避免无效状态触发音效。
        if (toggle != null && requireToggleOnForClick && !toggle.isOn)
        {
            return;
        }

        AudioEventSO evt = clickOverride != null
            ? clickOverride
            : AudioService.Instance.GetDefaultUiClickEvent(role);

        AudioService.Instance.PlayUI(evt);
    }

    /// <summary>
    /// 指针悬停时播放悬停音效。
    /// </summary>
    /// <param name="eventData">指针事件数据。</param>
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (muteThisControl || !enableHoverSound)
        {
            return;
        }

        AudioEventSO evt = hoverOverride != null
            ? hoverOverride
            : AudioService.Instance.GetDefaultUiHoverEvent(role);

        AudioService.Instance.PlayUI(evt);
    }
}
