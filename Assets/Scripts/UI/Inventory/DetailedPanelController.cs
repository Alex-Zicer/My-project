using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 悬停详情面板。挂在 DetailedPanel 预制体上（Canvas 根节点直接子节点）。
/// 由 BagPageController 在格子悬停时调用 Show/Hide。
/// 面板固定显示在格子左上方（空间不足时切换到右上方），带淡入淡出效果。
/// 注意：面板始终保持 active，仅通过 CanvasGroup.alpha 控制可见性，避免 SetActive 导致协程异常。
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

    private RectTransform _rectTransform;
    private CanvasGroup _canvasGroup;
    private Canvas _rootCanvas;
    private Coroutine _fadeCoroutine;

    private void Awake()
    {
        _rectTransform = GetComponent<RectTransform>();
        _canvasGroup = GetComponent<CanvasGroup>();
        _rootCanvas = GetComponentInParent<Canvas>();

        // 禁用所有子 Graphic 的射线检测，防止面板遮挡格子触发 OnPointerExit
        foreach (var graphic in GetComponentsInChildren<Graphic>(true))
            graphic.raycastTarget = false;

        // 初始不可见、不拦截射线，但保持 active 以便随时启动协程
        _canvasGroup.alpha = 0f;
        _canvasGroup.blocksRaycasts = false;
    }

    /// <summary> 显示面板，定位到格子左上方（或右上方），淡入显示。 </summary>
    public void Show(InventoryItem item, RectTransform slotRect)
    {
        if (_canvasGroup == null || _rectTransform == null) return;
        if (item?.ItemData == null) return;

        ItemDataBase data = item.ItemData;
        nameText.text = data.itemName;
        statsText.text = data.GetStatsText();
        descriptionText.text = data.description;
        iconImage.sprite = data.icon;
        iconImage.enabled = data.icon != null;

        // 强制重建布局以获得准确的面板尺寸，再计算位置
        LayoutRebuilder.ForceRebuildLayoutImmediate(_rectTransform);
        PositionNearSlot(slotRect);

        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(1f));
    }

    /// <summary> 淡出面板（不 SetActive，保持 active 状态）。 </summary>
    public void Hide()
    {
        if (_canvasGroup == null) return;
        if (_fadeCoroutine != null) StopCoroutine(_fadeCoroutine);
        _fadeCoroutine = StartCoroutine(FadeTo(0f));
    }

    // -------------------------------------------------------
    // 定位
    // -------------------------------------------------------

    private void PositionNearSlot(RectTransform slotRect)
    {
        if (_rootCanvas == null) return;

        RectTransform canvasRect = _rootCanvas.transform as RectTransform;
        Camera cam = _rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : _rootCanvas.worldCamera;

        Vector3[] corners = new Vector3[4];
        slotRect.GetWorldCorners(corners);
        // corners: 0=左下, 1=左上, 2=右上, 3=右下

        Vector2 slotUpperLeft  = WorldToCanvasLocal(canvasRect, cam, corners[1]);
        Vector2 slotUpperRight = WorldToCanvasLocal(canvasRect, cam, corners[2]);

        Vector2 size = _rectTransform.rect.size;

        // pivot 设为左上角：anchoredPosition 即面板左上角在 Canvas 本地坐标
        _rectTransform.pivot = new Vector2(0f, 1f);

        // 默认：面板放在格子左侧，顶部与格子顶部对齐
        Vector2 pos = new Vector2(slotUpperLeft.x - size.x, slotUpperLeft.y);

        // 若超出 Canvas 左侧边界，改为放在格子右侧
        if (pos.x < canvasRect.rect.xMin + 5f)
            pos = slotUpperRight;

        // 防止超出顶部边界（面板顶部 = pos.y）
        if (pos.y > canvasRect.rect.yMax - 5f)
            pos.y = canvasRect.rect.yMax - 5f;

        // 防止超出底部边界（面板底部 = pos.y - size.y）
        if (pos.y - size.y < canvasRect.rect.yMin + 5f)
            pos.y = canvasRect.rect.yMin + size.y + 5f;

        _rectTransform.anchoredPosition = pos;
    }

    private Vector2 WorldToCanvasLocal(RectTransform canvasRect, Camera cam, Vector3 worldPos)
    {
        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screenPoint, cam, out Vector2 localPoint);
        return localPoint;
    }

    // -------------------------------------------------------
    // 淡入淡出
    // -------------------------------------------------------

    private IEnumerator FadeTo(float target)
    {
        if (_canvasGroup == null) yield break;

        float start = _canvasGroup.alpha;
        float elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            if (_canvasGroup == null) yield break;

            elapsed += Time.unscaledDeltaTime;
            _canvasGroup.alpha = Mathf.Lerp(start, target, elapsed / fadeDuration);
            yield return null;
        }
        if (_canvasGroup != null)
            _canvasGroup.alpha = target;
    }
}
