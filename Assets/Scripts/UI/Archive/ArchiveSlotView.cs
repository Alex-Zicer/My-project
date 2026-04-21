using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 存档槽位视图：负责显示槽位编号与当前状态文案。
/// </summary>
[RequireComponent(typeof(Button))]
public class ArchiveSlotView : MonoBehaviour
{
    private const string SlotIdTextObjectName = "ArchiveSlotIDText";
    private const string SlotStateTextObjectName = "ArchiveSlotText";
    private const string LoadGameText = "载入存档";
    private const string NewGameText = "新游戏";

    [SerializeField] private TextMeshProUGUI _archiveSlotIdText;
    [SerializeField] private TextMeshProUGUI _archiveSlotText;
    [SerializeField] private Button _button;

    private int _slotIndex;
    private bool _hasSave;
    private Action<int, bool> _onSelected;

    /// <summary>
    /// 刷新当前槽位显示，并绑定点击回调。
    /// </summary>
    /// <param name="slotIndex">槽位编号（从 0 开始）。</param>
    /// <param name="hasSave">该槽位是否已有存档。</param>
    /// <param name="onSelected">槽位点击后的回调。</param>
    public void Setup(int slotIndex, bool hasSave, Action<int, bool> onSelected)
    {
        ResolveReferencesIfNeeded();

        _slotIndex = Mathf.Max(slotIndex, 0);
        _hasSave = hasSave;
        _onSelected = onSelected;

        if (_archiveSlotIdText != null)
        {
            _archiveSlotIdText.text = $"{_slotIndex + 1}.";
        }

        if (_archiveSlotText != null)
        {
            _archiveSlotText.text = hasSave ? LoadGameText : NewGameText;
        }
    }

    /// <summary>
    /// Unity 生命周期：初始化时自动查找文本引用。
    /// </summary>
    private void Awake()
    {
        ResolveReferencesIfNeeded();
        BindButtonClickIfNeeded();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下自动回填引用，减少手动拖拽。
    /// </summary>
    private void OnValidate()
    {
        ResolveReferencesIfNeeded();
    }
#endif

    /// <summary>
    /// 自动查找槽位编号文本与状态文本。
    /// </summary>
    private void ResolveReferencesIfNeeded()
    {
        if (_archiveSlotIdText == null)
        {
            _archiveSlotIdText = FindTextByName(SlotIdTextObjectName);
        }

        if (_archiveSlotText == null)
        {
            _archiveSlotText = FindTextByName(SlotStateTextObjectName);
        }

        if (_button == null)
        {
            _button = GetComponent<Button>();
        }
    }

    /// <summary>
    /// 绑定按钮点击事件，统一转发当前槽位信息。
    /// </summary>
    private void BindButtonClickIfNeeded()
    {
        if (_button == null)
        {
            return;
        }

        _button.onClick.RemoveListener(HandleButtonClicked);
        _button.onClick.AddListener(HandleButtonClicked);
    }

    /// <summary>
    /// 按钮点击后，把槽位编号与状态回传给页面控制器。
    /// </summary>
    private void HandleButtonClicked()
    {
        _onSelected?.Invoke(_slotIndex, _hasSave);
    }

    /// <summary>
    /// 按子物体名称查找 TMP 文本组件。
    /// </summary>
    /// <param name="childName">目标子物体名称。</param>
    /// <returns>找到则返回文本组件，否则返回 null。</returns>
    private TextMeshProUGUI FindTextByName(string childName)
    {
        if (string.IsNullOrWhiteSpace(childName))
        {
            return null;
        }

        Transform child = transform.Find(childName);
        return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
    }
}
