using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 存档页面控制器：负责固定生成 4 个槽位，并在页面打开时刷新显示。
/// </summary>
public class ArchivePageController : MonoBehaviour
{
    private const int DefaultSlotCount = 4;

    [SerializeField] private ArchiveSlotView _templateSlot;
    [SerializeField] private int _slotCount = DefaultSlotCount;

    private readonly List<ArchiveSlotView> _slotViews = new List<ArchiveSlotView>();
    private bool _isProcessingSelection;

    /// <summary>
    /// 手动刷新全部槽位显示。
    /// </summary>
    public void RefreshSlots()
    {
        _ = RefreshSlotsAsync();
    }

    /// <summary>
    /// 页面激活时确保槽位已生成并刷新状态。
    /// </summary>
    private void OnEnable()
    {
        _isProcessingSelection = false;
        RefreshSlots();
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下自动查找模板槽位，减少手动拖拽。
    /// </summary>
    private void OnValidate()
    {
        ResolveTemplateSlotIfNeeded();
        _slotCount = Mathf.Max(_slotCount, DefaultSlotCount);
    }
#endif

    /// <summary>
    /// 异步刷新全部槽位。
    /// </summary>
    private async Task RefreshSlotsAsync()
    {
        EnsureSlotViews();

        for (int slotIndex = 0; slotIndex < _slotViews.Count; slotIndex++)
        {
            ArchiveSlotView slotView = _slotViews[slotIndex];
            if (slotView == null)
            {
                continue;
            }

            bool hasSave = await SaveManager.Instance.HasSave(slotIndex);
            slotView.Setup(slotIndex, hasSave, HandleSlotSelected);
        }
    }

    /// <summary>
    /// 处理玩家点击槽位后的分流：有存档则读档，无存档则开始新游戏。
    /// </summary>
    /// <param name="slotIndex">被选中的槽位编号。</param>
    /// <param name="hasSave">该槽位当前是否存在存档。</param>
    private void HandleSlotSelected(int slotIndex, bool hasSave)
    {
        if (_isProcessingSelection)
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

    /// <summary>
    /// 确保页面下已存在固定数量的槽位视图。
    /// </summary>
    private void EnsureSlotViews()
    {
        ResolveTemplateSlotIfNeeded();
        RebuildSlotViewCache();

        if (_templateSlot == null)
        {
            Debug.LogWarning("[ArchivePageController] 缺少模板槽位，无法生成存档按钮。");
            return;
        }

        int targetSlotCount = Mathf.Max(_slotCount, DefaultSlotCount);
        while (_slotViews.Count < targetSlotCount)
        {
            ArchiveSlotView newSlot = Instantiate(_templateSlot, transform);
            newSlot.name = _templateSlot.name;
            _slotViews.Add(newSlot);
        }
    }

    /// <summary>
    /// 重新按层级顺序缓存页面下的全部槽位视图。
    /// </summary>
    private void RebuildSlotViewCache()
    {
        _slotViews.Clear();

        for (int childIndex = 0; childIndex < transform.childCount; childIndex++)
        {
            Transform child = transform.GetChild(childIndex);
            ArchiveSlotView slotView = child.GetComponent<ArchiveSlotView>();
            if (slotView != null)
            {
                _slotViews.Add(slotView);
            }
        }
    }

    /// <summary>
    /// 在当前页面子物体中自动查找模板槽位。
    /// </summary>
    private void ResolveTemplateSlotIfNeeded()
    {
        if (_templateSlot != null)
        {
            return;
        }

        _templateSlot = GetComponentInChildren<ArchiveSlotView>(true);
    }
}
