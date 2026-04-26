using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 存档页面控制器：负责刷新槽位，并处理读档、新游戏和删除存档。
/// </summary>
public class ArchivePageController : MonoBehaviour
{
    private const int DefaultSlotCount = 4;
    private const int NoPendingDeleteSlot = -1;

    [SerializeField] private ArchiveSlotView _templateSlot;
    [SerializeField] private int _slotCount = DefaultSlotCount;
    [SerializeField] private GameObject _confirmDialog;
    [SerializeField] private TextMeshProUGUI _confirmMessageText;
    [SerializeField] private Button _confirmButton;
    [SerializeField] private Button _cancelButton;

    private readonly List<ArchiveSlotView> _slotViews = new List<ArchiveSlotView>();
    private bool _isProcessingSelection;
    private bool _isDeleting;
    private int _pendingDeleteSlot = NoPendingDeleteSlot;

    /// <summary>
    /// 手动刷新全部槽位显示。
    /// </summary>
    public void RefreshSlots()
    {
        _ = RefreshSlotsAsync();
    }

    private void Awake()
    {
        if (_confirmButton != null)
        {
            _confirmButton.onClick.AddListener(HandleConfirmDeleteClicked);
        }

        if (_cancelButton != null)
        {
            _cancelButton.onClick.AddListener(HideConfirmDialog);
        }
    }

    private void OnEnable()
    {
        _isProcessingSelection = false;
        _isDeleting = false;
        _pendingDeleteSlot = NoPendingDeleteSlot;
        HideConfirmDialog();
        RefreshSlots();
    }

    /// <summary>
    /// 异步刷新全部槽位状态。
    /// </summary>
    private async Task RefreshSlotsAsync()
    {
        EnsureSlotViews();

        for (int i = 0; i < _slotViews.Count; i++)
        {
            bool hasSave = await SaveManager.Instance.HasSave(i);
            _slotViews[i].Setup(i, hasSave, HandleSlotSelected, HandleDeleteRequested);
        }
    }

    private void HandleSlotSelected(int slotIndex, bool hasSave)
    {
        if (_isProcessingSelection || _isDeleting || IsConfirmDialogVisible())
        {
            return;
        }

        if (UIManager.Instance == null)
        {
            Debug.LogWarning("[ArchivePageController] UIManager.Instance 为空，无法处理槽位点击。");
            return;
        }

        _isProcessingSelection = true;

        if (hasSave)
        {
            UIManager.Instance.LoadGamePlayFromSave(slotIndex);
            return;
        }

        UIManager.Instance.StartNewGameFromSlot(slotIndex);
    }

    private void HandleDeleteRequested(int slotIndex)
    {
        if (_isProcessingSelection || _isDeleting)
        {
            return;
        }

        if (_confirmDialog == null || _confirmButton == null || _cancelButton == null)
        {
            Debug.LogWarning("[ArchivePageController] 删除确认框引用未配置完整。");
            return;
        }

        _pendingDeleteSlot = slotIndex;

        if (_confirmMessageText != null)
        {
            _confirmMessageText.text = $"确定删除 {slotIndex + 1} 号存档吗？\n该操作不可撤销。";
        }

        _confirmDialog.SetActive(true);
    }

    private async void HandleConfirmDeleteClicked()
    {
        if (_pendingDeleteSlot < 0 || _isDeleting)
        {
            return;
        }

        int slotIndex = _pendingDeleteSlot;
        HideConfirmDialog();
        _isDeleting = true;

        try
        {
            await SaveManager.Instance.Delete(slotIndex);
            await RefreshSlotsAsync();
        }
        finally
        {
            _isDeleting = false;
        }
    }

    private void HideConfirmDialog()
    {
        _pendingDeleteSlot = NoPendingDeleteSlot;

        if (_confirmDialog != null)
        {
            _confirmDialog.SetActive(false);
        }
    }

    /// <summary>
    /// 确保页面下已存在固定数量的槽位视图。
    /// </summary>
    private void EnsureSlotViews()
    {
        _slotViews.Clear();

        for (int i = 0; i < transform.childCount; i++)
        {
            ArchiveSlotView slotView = transform.GetChild(i).GetComponent<ArchiveSlotView>();
            if (slotView != null)
            {
                _slotViews.Add(slotView);
            }
        }

        if (_templateSlot == null)
        {
            Debug.LogWarning("[ArchivePageController] 未配置模板槽位。");
            return;
        }

        int targetCount = Mathf.Max(_slotCount, DefaultSlotCount);
        while (_slotViews.Count < targetCount)
        {
            ArchiveSlotView slotView = Instantiate(_templateSlot, transform);
            slotView.name = _templateSlot.name;
            _slotViews.Add(slotView);
        }
    }

    private bool IsConfirmDialogVisible()
    {
        return _confirmDialog != null && _confirmDialog.activeSelf;
    }
}
