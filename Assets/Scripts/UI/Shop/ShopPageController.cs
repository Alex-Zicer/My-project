using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 商店页面总控制器。负责：
/// 刷新商店列表、维护当前选中项、驱动详情面板显示，并处理购买逻辑。
/// 当前阶段只实现买入，不负责卖出。
/// </summary>
public class ShopPageController : MonoBehaviour
{
    [Header("商店数据")]
    [Tooltip("当前商店上架的物品列表，按 Inspector 顺序展示。")]
    [SerializeField] private List<ItemDataBase> shopItems = new List<ItemDataBase>();

    [Header("列表配置")]
    [SerializeField] private ShopItemSlotView slotPrefab;
    [SerializeField] private Transform contentRoot;

    [Header("详情与操作")]
    [SerializeField] private DetailedPanelController detailedPanel;
    [SerializeField] private Button buyButton;
    [SerializeField] private TextMeshProUGUI currentMoneyText;

    [Header("运行时依赖")]
    [Tooltip("玩家金钱组件。商店页通过它判断是否够钱并执行扣款。")]
    [SerializeField] private Money playerMoney;

    private readonly List<ShopItemSlotView> _activeSlots = new List<ShopItemSlotView>();

    // 当前选中的商店物品，用于刷新详情与执行购买。
    private ItemDataBase _selectedItemData;
    // 当前选中的商店格子，用于切换高亮。
    private ShopItemSlotView _selectedSlot;

    private void OnEnable()
    {
        if (buyButton != null)
        {
            buyButton.onClick.AddListener(HandleBuyClicked);
        }

        if (playerMoney != null)
        {
            playerMoney.OnMoneyChanged += HandleMoneyChanged;
        }

        if (detailedPanel != null)
        {
            if (!detailedPanel.gameObject.activeSelf)
            {
                detailedPanel.gameObject.SetActive(true);
            }

            detailedPanel.Hide();
        }

        Refresh();
    }

    private void OnDisable()
    {
        if (buyButton != null)
        {
            buyButton.onClick.RemoveListener(HandleBuyClicked);
        }

        if (playerMoney != null)
        {
            playerMoney.OnMoneyChanged -= HandleMoneyChanged;
        }

        ClearSelection();
        ClearSlots();
    }

    /// <summary>
    /// 刷新整个商店页面：重建列表、清空旧选中，并同步按钮与金钱文本状态。
    /// </summary>
    private void Refresh()
    {
        ClearSelection();
        ClearSlots();
        BuildSlots();
        RefreshMoneyText();
        RefreshBuyButtonState();
    }

    /// <summary>
    /// 根据当前配置的商店物品列表生成右侧滚动列表。
    /// </summary>
    private void BuildSlots()
    {
        if (slotPrefab == null || contentRoot == null)
        {
            return;
        }

        for (int i = 0; i < shopItems.Count; i++)
        {
            ItemDataBase itemData = shopItems[i];
            if (itemData == null)
            {
                continue;
            }

            ShopItemSlotView slot = Instantiate(slotPrefab, contentRoot);
            slot.Bind(itemData);
            slot.OnClicked += HandleSlotClicked;
            _activeSlots.Add(slot);
        }
    }

    /// <summary>
    /// 销毁当前商店页运行时生成的所有格子，避免页面重复打开时列表叠加。
    /// </summary>
    private void ClearSlots()
    {
        for (int i = _activeSlots.Count - 1; i >= 0; i--)
        {
            ShopItemSlotView slot = _activeSlots[i];
            if (slot == null)
            {
                continue;
            }

            slot.Release();
            Destroy(slot.gameObject);
        }

        _activeSlots.Clear();
    }

    /// <summary>
    /// 处理商店格子点击，切换当前选中项并刷新详情。
    /// </summary>
    /// <param name="itemData">被点击的物品数据。</param>
    /// <param name="slot">被点击的格子。</param>
    private void HandleSlotClicked(ItemDataBase itemData, ShopItemSlotView slot)
    {
        if (itemData == null || slot == null)
        {
            return;
        }

        _selectedItemData = itemData;
        ApplySelectedSlot(slot);

        if (detailedPanel != null)
        {
            detailedPanel.ShowSelected(itemData);
        }

        RefreshBuyButtonState();
    }

    /// <summary>
    /// 应用当前选中的格子高亮，并取消旧格子的高亮。
    /// </summary>
    /// <param name="slot">要高亮显示的格子。</param>
    private void ApplySelectedSlot(ShopItemSlotView slot)
    {
        if (_selectedSlot != null && _selectedSlot != slot)
        {
            _selectedSlot.SetSelected(false);
        }

        _selectedSlot = slot;
        _selectedSlot.SetSelected(true);
    }

    /// <summary>
    /// 处理购买按钮点击：扣钱、加入背包，并在成功后同步刷新按钮状态。
    /// </summary>
    private void HandleBuyClicked()
    {
        if (_selectedItemData == null || playerMoney == null || Inventory.Instance == null)
        {
            return;
        }

        int buyPrice = Mathf.Max(0, _selectedItemData.buyPrice);
        if (!playerMoney.CanAfford(buyPrice))
        {
            RefreshBuyButtonState();
            return;
        }

        // 先扣钱再加到背包，保证购买流程的主状态变化顺序清晰可控。
        if (!playerMoney.SpendMoney(buyPrice))
        {
            RefreshBuyButtonState();
            return;
        }

        // 当前背包没有容量上限，但这里仍保留失败兜底，避免未来扩展时出现扣钱成功却未入包。
        bool added = Inventory.Instance.AddItem(_selectedItemData, 1);
        if (!added)
        {
            playerMoney.AddMoney(buyPrice);
            RefreshBuyButtonState();
            return;
        }

        RefreshMoneyText();
        RefreshBuyButtonState();
    }

    /// <summary>
    /// 金钱变化时同步刷新页面显示与购买按钮状态。
    /// </summary>
    /// <param name="currentMoney">当前钱数。</param>
    /// <param name="delta">变化量。</param>
    private void HandleMoneyChanged(int currentMoney, int delta)
    {
        RefreshMoneyText();
        RefreshBuyButtonState();
    }

    /// <summary>
    /// 按当前金钱与选中项状态刷新购买按钮是否可交互。
    /// </summary>
    private void RefreshBuyButtonState()
    {
        if (buyButton == null)
        {
            return;
        }

        bool canBuy = _selectedItemData != null &&
                      playerMoney != null &&
                      playerMoney.CanAfford(Mathf.Max(0, _selectedItemData.buyPrice));

        buyButton.interactable = canBuy;
    }

    /// <summary>
    /// 刷新页面中的当前金钱文本。若未绑定文本则跳过。
    /// </summary>
    private void RefreshMoneyText()
    {
        if (currentMoneyText == null || playerMoney == null)
        {
            return;
        }

        currentMoneyText.text = playerMoney.CurrentMoney.ToString();
    }

    /// <summary>
    /// 清空当前选中项、高亮与详情显示。
    /// </summary>
    private void ClearSelection()
    {
        if (_selectedSlot != null)
        {
            _selectedSlot.SetSelected(false);
        }

        _selectedSlot = null;
        _selectedItemData = null;

        if (detailedPanel != null)
        {
            detailedPanel.Hide();
        }
    }
}
