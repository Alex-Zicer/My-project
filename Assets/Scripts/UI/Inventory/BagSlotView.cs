using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 背包格子视图。负责显示单个物品的图标与数量，
/// 并向外暴露悬停、点击事件供背包页控制器订阅。
/// </summary>
public class BagSlotView : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler, IPointerClickHandler
{
    [SerializeField] private Image selectionHighlightImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI countText;

    /// <summary> 鼠标悬停进入时触发，携带物品数据和格子自身 RectTransform。 </summary>
    public event Action<InventoryItem, RectTransform> OnHoverEnter;
    /// <summary> 鼠标悬停离开时触发。 </summary>
    public event Action OnHoverExit;
    /// <summary> 鼠标点击格子时触发，携带当前物品和格子视图自身。 </summary>
    public event Action<InventoryItem, BagSlotView> OnClicked;

    private InventoryItem _boundItem;

    /// <summary>
    /// 绑定物品数据并刷新显示。由对象池借出后立即调用。
    /// </summary>
    public void Bind(InventoryItem item)
    {
        _boundItem = item;
        SetSelected(false);

        if (item?.ItemData != null)
        {
            iconImage.sprite = item.ItemData.icon;
            iconImage.enabled = true;
        }
        else
        {
            iconImage.sprite = null;
            iconImage.enabled = false;
        }

        // 可叠加物品显示数量，不可叠加或数量为 1 时隐藏数量文本
        bool showCount = item != null && item.IsStackable;
        countText.text = showCount ? item.Count.ToString() : string.Empty;
        countText.enabled = showCount;
    }

    /// <summary>
    /// 设置格子的选中高亮状态。
    /// </summary>
    /// <param name="isSelected">是否显示选中高亮。</param>
    public void SetSelected(bool isSelected)
    {
        if (selectionHighlightImage == null) return;
        selectionHighlightImage.enabled = isSelected;
    }

    /// <summary>
    /// 清空格子数据。归还对象池前调用，防止旧数据残留。
    /// </summary>
    public void Release()
    {
        _boundItem = null;
        SetSelected(false);
        iconImage.sprite = null;
        iconImage.enabled = false;
        countText.text = string.Empty;
        countText.enabled = false;

        // 清除悬停事件订阅，防止归还后仍持有外部引用
        OnHoverEnter = null;
        OnHoverExit = null;
        OnClicked = null;
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (_boundItem != null)
            OnHoverEnter?.Invoke(_boundItem, transform as RectTransform);
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        OnHoverExit?.Invoke();
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_boundItem == null) return;
        OnClicked?.Invoke(_boundItem, this);
    }
}
