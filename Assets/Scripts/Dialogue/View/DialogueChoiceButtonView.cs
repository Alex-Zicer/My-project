using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DialogueChoiceButtonView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private TextMeshProUGUI label;

    // 当前按钮对应的选项索引。
    private int _choiceIndex;

    // 选项点击回调。
    private Action<int> _clickHandler;

    /// <summary>
    /// 在编辑器中自动补齐默认引用。
    /// </summary>
    private void Reset()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// 初始化组件并确保运行时状态有效。
    /// </summary>
    private void Awake()
    {
        if (button == null)
        {
            button = GetComponent<Button>();
        }

        if (label == null)
        {
            label = GetComponentInChildren<TextMeshProUGUI>();
        }
    }

    /// <summary>
    /// 初始化选项按钮文本与点击回调。
    /// </summary>
    public void Setup(int choiceIndex, string text, Action<int> clickHandler)
    {
        _choiceIndex = choiceIndex;
        _clickHandler = clickHandler;

        if (label != null)
        {
            label.text = text ?? string.Empty;
        }

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(OnClick);
        }
    }

    /// <summary>
    /// 配置当前按钮的上下导航关系。
    /// </summary>
    public void SetNavigation(Selectable selectOnUp, Selectable selectOnDown)
    {
        if (button == null)
        {
            return;
        }

        Navigation navigation = button.navigation;
        navigation.mode = Navigation.Mode.Explicit;
        navigation.selectOnUp = selectOnUp;
        navigation.selectOnDown = selectOnDown;
        navigation.selectOnLeft = null;
        navigation.selectOnRight = null;
        button.navigation = navigation;
    }

    /// <summary>
    /// 选中当前按钮。
    /// </summary>
    public void Select()
    {
        if (button == null)
        {
            return;
        }

        button.Select();
        if (EventSystem.current != null)
        {
            EventSystem.current.SetSelectedGameObject(button.gameObject);
        }
    }

    /// <summary>
    /// 触发当前按钮提交。
    /// </summary>
    public void Submit()
    {
        if (button == null || !button.IsInteractable())
        {
            return;
        }

        button.onClick.Invoke();
    }

    /// <summary>
    /// 获取当前按钮对应的可选控件。
    /// </summary>
    public Selectable GetSelectable()
    {
        return button;
    }

    /// <summary>
    /// 转发选项点击事件。
    /// </summary>
    private void OnClick()
    {
        _clickHandler?.Invoke(_choiceIndex);
    }
}