using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 详情面板控制器。
/// 挂在 DetailedPanel 预制体上（Canvas 根节点直接子节点）。
/// 由 BagPageController 在格子选中时调用 ShowSelected/Hide。
/// 面板位置由场景中手动摆放，代码只负责刷新内容与播放淡入淡出效果。
/// 当前不额外处理射线相关设置，显示与隐藏只通过 CanvasGroup.alpha 控制。
/// </summary>
[RequireComponent(typeof(CanvasGroup))]
public class DetailedPanelController : MonoBehaviour
{
    [Header("UI 引用")]
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI statsText;
    [SerializeField] private TextMeshProUGUI descriptionText;
    [SerializeField] private Image iconImage;

    [Header("动画")]
    [SerializeField] private float fadeDuration = 0.12f;

    private CanvasGroup _canvasGroup;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _canvasGroup = GetComponent<CanvasGroup>();
        _canvasGroup.alpha = 0f;
    }

    /// <summary>
    /// 按固定位置显示当前选中的物品详情。
    /// </summary>
    /// <param name="item">当前选中的物品。</param>
    public void ShowSelected(InventoryItem item)
    {
        if (_canvasGroup == null) return;
        if (item?.ItemData == null) return;

        ShowInternal(item.ItemData);
    }

    /// <summary>
    /// 按固定位置显示指定静态物品数据的详情。
    /// 供商店页面直接复用详情面板，无需额外构造运行时背包条目。
    /// </summary>
    /// <param name="itemData">要显示的物品数据。</param>
    public void ShowSelected(ItemDataBase itemData)
    {
        if (_canvasGroup == null) return;
        if (itemData == null) return;

        ShowInternal(itemData);
    }

    /// <summary>
    /// 隐藏面板，淡出效果。
    /// 不使用 SetActive，保持 active 状态以便协程正常执行。
    /// </summary>
    public void Hide()
    {
        if (_canvasGroup == null) return;
        // 页面关闭时，详情面板可能已经跟着父节点一起失活。
        // 这时不能再启动协程，直接把透明度置为 0，避免重复在调用方分散判断。
        if (!gameObject.activeInHierarchy)
        {
            ApplyVisibility(0f);
            return;
        }

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(0f));
    }

    /// <summary>
    /// 刷新当前详情面板的显示内容。
    /// </summary>
    /// <param name="data">要显示的物品静态数据。</param>
    private void ShowInternal(ItemDataBase data)
    {
        nameText.text = data.itemName;
        statsText.text = FormatStatsText(data.GetStatsText());
        descriptionText.text = data.description;
        iconImage.sprite = data.icon;
        iconImage.enabled = data.icon != null;

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(1f));
    }

    /// <summary>
    /// 统一格式化物品属性文本。
    /// 当前约定：若数据层已经主动换行，则原样保留；
    /// 否则把用于分隔字段的连续双空格替换为换行，统一详情面板显示样式。
    /// </summary>
    /// <param name="rawStatsText">数据层返回的原始属性文本。</param>
    /// <returns>适合详情面板展示的格式化文本。</returns>
    private static string FormatStatsText(string rawStatsText)
    {
        if (string.IsNullOrWhiteSpace(rawStatsText))
        {
            return string.Empty;
        }

        // 已经显式换行的文本不再二次处理，避免破坏个别物品的自定义排版。
        if (rawStatsText.Contains("\n"))
        {
            return rawStatsText;
        }

        // 当前项目里的属性字段通常用双空格分隔，这里统一替换为换行输出。
        return rawStatsText.Replace("  ", "\n");
    }

    // -------------------------------------------------------
    // 淡入淡出
    // -------------------------------------------------------

    /// <summary>
    /// 淡入淡出协程。
    /// 使用 unscaledDeltaTime 确保在游戏暂停时动画仍能正常播放。
    /// </summary>
    /// <param name="target">目标透明度，1f 为完全显示，0f 为完全隐藏</param>
    private IEnumerator FadeTo(float target)
    {
        if (_canvasGroup == null) yield break;

        float start = _canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (_canvasGroup == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            ApplyVisibility(Mathf.Lerp(start, target, elapsed / fadeDuration));
            yield return null;
        }
        if (_canvasGroup != null)
            ApplyVisibility(target);
    }

    /// <summary>
    /// 统一应用详情面板的可见性状态。
    /// </summary>
    /// <param name="alpha">目标透明度。</param>
    private void ApplyVisibility(float alpha)
    {
        if (_canvasGroup == null) return;

        _canvasGroup.alpha = alpha;
    }
}
