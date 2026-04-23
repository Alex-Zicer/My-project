using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

/// <summary>
/// 商店物品格子视图。负责显示图标、买入价格与选中高亮，
/// 并向外暴露点击事件供商店页面控制器订阅。
/// </summary>
public class ShopItemSlotView : MonoBehaviour, IPointerClickHandler
{
    [SerializeField] private Image selectionHighlightImage;
    [SerializeField] private Image iconImage;
    [SerializeField] private TextMeshProUGUI priceText;

    /// <summary> 鼠标点击格子时触发，携带当前物品数据和格子视图自身。 </summary>
    public event Action<ItemDataBase, ShopItemSlotView> OnClicked;

    private ItemDataBase _boundItemData;

    /// <summary>
    /// 绑定商店物品数据并刷新显示。由对象池或页面初始化后调用。
    /// </summary>
    /// <param name="itemData">要显示的物品数据。</param>
    public void Bind(ItemDataBase itemData)
    {
        _boundItemData = itemData;
        SetSelected(false);

        if (itemData != null)
        {
            iconImage.sprite = itemData.icon;
            iconImage.enabled = itemData.icon != null;

            // 商店列表阶段直接在格子上显示买入价格，方便玩家横向比较。
            priceText.text = itemData.buyPrice.ToString();
            priceText.enabled = true;
            return;
        }

        iconImage.sprite = null;
        iconImage.enabled = false;
        priceText.text = string.Empty;
        priceText.enabled = false;
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
    /// 清空格子数据。归还对象池或页面关闭前调用，防止旧数据残留。
    /// </summary>
    public void Release()
    {
        _boundItemData = null;
        SetSelected(false);
        iconImage.sprite = null;
        iconImage.enabled = false;
        priceText.text = string.Empty;
        priceText.enabled = false;
        OnClicked = null;
    }

    /// <summary>
    /// 处理格子点击并把当前绑定的商店物品抛给外部控制器。
    /// </summary>
    /// <param name="eventData">指针事件数据。</param>
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_boundItemData == null) return;
        OnClicked?.Invoke(_boundItemData, this);
    }
}
