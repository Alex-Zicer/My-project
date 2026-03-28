using UnityEngine;

/// <summary>
/// UI 默认音效配置：按角色提供点击/悬停默认事件。
/// </summary>
[CreateAssetMenu(fileName = "UIDefaultAudioProfile", menuName = "Audio/UI Default Audio Profile")]
public class UIDefaultAudioProfileSO : ScriptableObject
{
    // Default 角色点击事件。
    [Header("Default")]
    [SerializeField] private AudioEventSO defaultClickEvent;

    // Default 角色悬停事件。
    [SerializeField] private AudioEventSO defaultHoverEvent;

    // Important 角色点击事件。
    [Header("Important")]
    [SerializeField] private AudioEventSO importantClickEvent;

    // Important 角色悬停事件。
    [SerializeField] private AudioEventSO importantHoverEvent;

    // Back 角色点击事件。
    [Header("Back")]
    [SerializeField] private AudioEventSO backClickEvent;

    // Back 角色悬停事件。
    [SerializeField] private AudioEventSO backHoverEvent;

    // Tab 角色点击事件。
    [Header("Tab")]
    [SerializeField] private AudioEventSO tabClickEvent;

    // Tab 角色悬停事件。
    [SerializeField] private AudioEventSO tabHoverEvent;

    /// <summary>
    /// 根据角色获取默认点击事件。
    /// </summary>
    /// <param name="role">UI 音效角色。</param>
    /// <returns>命中事件，若为空则回退默认点击事件。</returns>
    public AudioEventSO GetClickEvent(UIAudioRole role)
    {
        switch (role)
        {
            case UIAudioRole.Important:
                return importantClickEvent != null ? importantClickEvent : defaultClickEvent;
            case UIAudioRole.Back:
                return backClickEvent != null ? backClickEvent : defaultClickEvent;
            case UIAudioRole.Tab:
                return tabClickEvent != null ? tabClickEvent : defaultClickEvent;
            default:
                return defaultClickEvent;
        }
    }

    /// <summary>
    /// 根据角色获取默认悬停事件。
    /// </summary>
    /// <param name="role">UI 音效角色。</param>
    /// <returns>命中事件，若为空则回退默认悬停事件。</returns>
    public AudioEventSO GetHoverEvent(UIAudioRole role)
    {
        switch (role)
        {
            case UIAudioRole.Important:
                return importantHoverEvent != null ? importantHoverEvent : defaultHoverEvent;
            case UIAudioRole.Back:
                return backHoverEvent != null ? backHoverEvent : defaultHoverEvent;
            case UIAudioRole.Tab:
                return tabHoverEvent != null ? tabHoverEvent : defaultHoverEvent;
            default:
                return defaultHoverEvent;
        }
    }
}
