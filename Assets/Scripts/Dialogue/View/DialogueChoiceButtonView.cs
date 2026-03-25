using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 单个选项按钮视图：
// 负责把“选项文本 + 索引”绑定到 UI Button 点击事件。
public class DialogueChoiceButtonView : MonoBehaviour
{
    // 实际响应点击的按钮组件。
    [SerializeField] private Button button;
    // 显示选项内容的文本组件。
    [SerializeField] private TextMeshProUGUI label;

    // 当前按钮对应的选项索引。
    private int _choiceIndex;
    // 点击后回调给上层（通常是 DialoguePageController）。
    private Action<int> _clickHandler;

    private void Reset()
    {
        // 在编辑器重置时自动尝试抓取同物体组件，减少手工绑定。
        if (button == null) button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
    }

    private void Awake()
    {
        // 运行时兜底：如果未在 Inspector 绑定，自动查找。
        if (button == null) button = GetComponent<Button>();
        if (label == null) label = GetComponentInChildren<TextMeshProUGUI>();
    }

    // 初始化按钮显示与点击行为。
    public void Setup(int choiceIndex, string text, Action<int> clickHandler)
    {
        _choiceIndex = choiceIndex;
        _clickHandler = clickHandler;

        if (label != null) label.text = text ?? string.Empty;
        if (button != null)
        {
            // 先清再绑，避免复用按钮时重复订阅。
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        // 将索引回传给上层，具体跳转逻辑由运行层决定。
        _clickHandler?.Invoke(_choiceIndex);
    }
}
