﻿﻿﻿﻿﻿﻿﻿using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 背包页面总控制器。挂在 BagPage GameObject 上，负责：
/// 订阅背包数据变化、驱动分类筛选、通过对象池刷新格子列表。
/// </summary>
public class BagPageController : MonoBehaviour
{
    [Header("对象池")]
    [SerializeField] private BagSlotPool pool;

    [Header("格子挂载节点")]
    [Tooltip("ScrollRect 的 Content 节点")]
    [SerializeField] private Transform contentRoot;

    [Header("布局配置")]
    [Tooltip("每行格子数，需与 GridLayoutGroup 保持一致")]
    [SerializeField] private int columnsPerRow = 6;
    [Tooltip("末尾至少保留几行空格子")]
    [SerializeField] private int minEmptyRows = 1;

    [Header("详情面板")]
    [SerializeField] private DetailedPanelController detailedPanel;

    [Header("操作面板")]
    [SerializeField] private GameObject actionPanelRoot;
    [SerializeField] private Button sellButton;
    [SerializeField] private Button equipButton;
    [SerializeField] private Button dropButton;

    // 当前筛选类型，null 表示显示全部
    private ItemType? _currentFilter = null;
    // 当前选中的格子视图，用于切换高亮。
    private BagSlotView _selectedSlot;
    // 当前选中的物品。操作面板点击按钮时从这里读取目标条目。
    private InventoryItem _selectedItem;

    // -------------------------------------------------------
    // Unity 生命周期
    // -------------------------------------------------------

    private void OnEnable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += Refresh;

        if (dropButton != null)
        {
            dropButton.onClick.AddListener(HandleDropClicked);
        }

        if (actionPanelRoot != null)
        {
            actionPanelRoot.SetActive(false);
        }

        if (detailedPanel != null)
        {
            if (!detailedPanel.gameObject.activeSelf)
                detailedPanel.gameObject.SetActive(true);
            detailedPanel.Hide();
        }
        Refresh();
    }

    private void OnDisable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= Refresh;

        if (dropButton != null)
        {
            dropButton.onClick.RemoveListener(HandleDropClicked);
        }

        _selectedSlot = null;
        _selectedItem = null;
        pool.ReturnAll();
    }

    // -------------------------------------------------------
    // 分类按钮（在 Inspector 中绑定到对应 Button.OnClick）
    // -------------------------------------------------------

    /// <summary> 显示全部物品。 </summary>
    public void ShowAll()
    {
        _currentFilter = null;
        Refresh();
    }

    /// <summary> 只显示武器。 </summary>
    public void ShowWeapons()
    {
        _currentFilter = ItemType.Weapon;
        Refresh();
    }

    /// <summary> 只显示防具。 </summary>
    public void ShowArmors()
    {
        _currentFilter = ItemType.Armor;
        Refresh();
    }

    /// <summary> 只显示杂物/材料。 </summary>
    public void ShowMisc()
    {
        _currentFilter = ItemType.Misc;
        Refresh();
    }

    /// <summary> 整理按钮：对背包物品排序，UI 通过 OnInventoryChanged 自动刷新。 </summary>
    public void OnSortButton()
    {
        Inventory.Instance?.Sort();
    }

    // -------------------------------------------------------
    // 核心刷新
    // -------------------------------------------------------

    /// <summary>
    /// 归还所有格子，按当前筛选条件重新从对象池取格子并绑定数据。
    /// 背包数据变化或切换分类时调用。
    /// </summary>
    private void Refresh()
    {
        // 当前阶段的交互约定：只要列表发生刷新（如切换分类、排序、数量变化），
        // 就主动清空选中高亮和详情面板，让玩家重新点击选择，避免旧选择在新列表中造成误解。
        HideActionPanel();

        if (detailedPanel != null)
        {
            detailedPanel.Hide();
        }

        _selectedSlot = null;
        _selectedItem = null;
        pool.ReturnAll();

        if (Inventory.Instance == null) return;

        List<InventoryItem> data = _currentFilter.HasValue
            ? Inventory.Instance.GetItemsByType(_currentFilter.Value)
            : Inventory.Instance.GetAllItems();

        // 有数据的格子
        foreach (InventoryItem item in data)
        {
            BagSlotView slot = pool.Get(contentRoot);
            slot.Bind(item);
            slot.OnClicked += HandleSlotClicked;
        }

        // 补空格子：总格子数取 "已填充行+minEmptyRows 行" 与 preWarmCount 两者的较大值，
        // 保证页面始终至少显示 preWarmCount 个格子（默认 36）。
        int filledRows = Mathf.CeilToInt((float)data.Count / columnsPerRow);
        int totalSlots = Mathf.Max((filledRows + minEmptyRows) * columnsPerRow, pool.PreWarmCount);
        int emptyCount = totalSlots - data.Count;
        for (int i = 0; i < emptyCount; i++)
        {
            BagSlotView slot = pool.Get(contentRoot);
            slot.Bind(null);
        }
    }

    /// <summary>
    /// 处理格子点击，切换当前选中项并显示高亮。
    /// </summary>
    /// <param name="item">被点击的物品。</param>
    /// <param name="slot">被点击的格子。</param>
    private void HandleSlotClicked(InventoryItem item, BagSlotView slot)
    {
        if (item == null || slot == null) return;

        _selectedItem = item;
        ApplySelectedSlot(slot);

        if (detailedPanel != null)
        {
            detailedPanel.ShowSelected(item);
        }

        ShowActionPanelForItem(item);
    }

    /// <summary>
    /// 应用当前选中的格子高亮，并取消旧格子的高亮。
    /// </summary>
    /// <param name="slot">要高亮显示的格子。</param>
    private void ApplySelectedSlot(BagSlotView slot)
    {
        if (_selectedSlot != null && _selectedSlot != slot)
        {
            _selectedSlot.SetSelected(false);
        }

        _selectedSlot = slot;
        _selectedSlot.SetSelected(true);
    }

    /// <summary>
    /// 处理操作面板中的丢弃请求。
    /// 杂物按 1 个消耗；不可叠加装备按实例 ID 精确移除。
    /// </summary>
    private void HandleDropClicked()
    {
        if (_selectedItem == null || Inventory.Instance == null) return;
        if (_selectedItem.ItemData == null) return;

        bool removed = _selectedItem.IsStackable
            ? Inventory.Instance.RemoveItem(_selectedItem.ItemData, 1)
            : Inventory.Instance.RemoveItemByInstanceId(_selectedItem.InstanceId);

        // 只有真正移除成功时才主动隐藏面板，随后会由 OnInventoryChanged 触发完整刷新。
        if (!removed) return;

        HideActionPanel();

        if (detailedPanel != null)
        {
            detailedPanel.Hide();
        }
    }

    /// <summary>
    /// 根据当前选中物品类型切换操作面板按钮状态。
    /// 杂物显示“出售 + 丢弃”，装备显示“装备 + 丢弃”。
    /// 其中出售和装备当前阶段只展示，不接实际功能。
    /// </summary>
    /// <param name="item">当前选中的物品。</param>
    private void ShowActionPanelForItem(InventoryItem item)
    {
        if (actionPanelRoot == null || item?.ItemData == null)
        {
            HideActionPanel();
            return;
        }

        bool isMiscItem = item.ItemData.itemType == ItemType.Misc;

        // 用 SetActive 控制按钮是否参与 LayoutGroup 排版，避免隐藏按钮留下空位。
        if (sellButton != null)
        {
            sellButton.gameObject.SetActive(isMiscItem);
            sellButton.interactable = false;
        }

        if (equipButton != null)
        {
            equipButton.gameObject.SetActive(!isMiscItem);
            equipButton.interactable = false;
        }

        if (dropButton != null)
        {
            dropButton.gameObject.SetActive(true);
            dropButton.interactable = true;
        }

        actionPanelRoot.SetActive(true);
    }

    /// <summary>
    /// 隐藏整个操作面板。
    /// </summary>
    private void HideActionPanel()
    {
        if (actionPanelRoot != null)
        {
            actionPanelRoot.SetActive(false);
        }
    }

}
