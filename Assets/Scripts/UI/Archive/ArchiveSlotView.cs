using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 存档槽位视图：负责显示槽位信息并转发点击事件。
/// </summary>
[RequireComponent(typeof(Button))]
public class ArchiveSlotView : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI _slotIdText;
    [SerializeField] private TextMeshProUGUI _slotStateText;
    [SerializeField] private Button _deleteButton;

    private Button _button;
    private int _slotIndex;
    private bool _hasSave;
    private Action<int, bool> _onSelected;
    private Action<int> _onDeleteRequested;

    /// <summary>
    /// 刷新槽位显示并更新回调。
    /// </summary>
    /// <param name="slotIndex">槽位编号。</param>
    /// <param name="hasSave">是否已有存档。</param>
    /// <param name="onSelected">点击槽位时的回调。</param>
    /// <param name="onDeleteRequested">点击删除按钮时的回调。</param>
    public void Setup(int slotIndex, bool hasSave, Action<int, bool> onSelected, Action<int> onDeleteRequested)
    {
        _slotIndex = slotIndex;
        _hasSave = hasSave;
        _onSelected = onSelected;
        _onDeleteRequested = onDeleteRequested;

        if (_slotIdText != null)
        {
            _slotIdText.text = $"{slotIndex + 1}.";
        }

        if (_slotStateText != null)
        {
            _slotStateText.text = hasSave ? "读取存档" : "新游戏";
        }

        if (_deleteButton != null)
        {
            _deleteButton.gameObject.SetActive(hasSave);
        }
    }

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.AddListener(HandleButtonClicked);

        if (_deleteButton != null)
        {
            _deleteButton.onClick.AddListener(HandleDeleteButtonClicked);
        }
    }

    private void HandleButtonClicked()
    {
        _onSelected?.Invoke(_slotIndex, _hasSave);
    }

    private void HandleDeleteButtonClicked()
    {
        _onDeleteRequested?.Invoke(_slotIndex);
    }
}
