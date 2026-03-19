using System.Collections.Generic;
using UnityEngine;

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

    // 当前筛选类型，null 表示显示全部
    private ItemType? _currentFilter = null;

    // -------------------------------------------------------
    // Unity 生命周期
    // -------------------------------------------------------

    private void OnEnable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged += Refresh;

        Refresh();
    }

    private void OnDisable()
    {
        if (Inventory.Instance != null)
            Inventory.Instance.OnInventoryChanged -= Refresh;

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

    // -------------------------------------------------------
    // 核心刷新
    // -------------------------------------------------------

    /// <summary>
    /// 归还所有格子，按当前筛选条件重新从对象池取格子并绑定数据。
    /// 背包数据变化或切换分类时调用。
    /// </summary>
    private void Refresh()
    {
        pool.ReturnAll();

        if (Inventory.Instance == null) return;

        List<InventoryItem> data = _currentFilter.HasValue
            ? Inventory.Instance.GetItemsByType(_currentFilter.Value)
            : Inventory.Instance.GetAllItems();

        foreach (InventoryItem item in data)
        {
            BagSlotView slot = pool.Get(contentRoot);
            slot.Bind(item);
            // 悬停事件预留接口，后续接入 TooltipPanel 时在此订阅
            // slot.OnHoverEnter += tooltip.Show;
            // slot.OnHoverExit  += tooltip.Hide;
        }
    }
}
