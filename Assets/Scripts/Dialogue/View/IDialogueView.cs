using System;
using System.Collections.Generic;
using UnityEngine;

public interface IDialogueView
{
    // OnNextRequested 事件。
event Action OnNextRequested;
    // OnChoiceSelected 事件。
event Action<int> OnChoiceSelected;

/// <summary>
/// 打开界面并同步内部状态。
/// </summary>
void Open();
/// <summary>
/// 关闭界面并清理展示数据。
/// </summary>
void Close();
/// <summary>
/// 设置说话者名称与头像显示。
/// </summary>
void SetSpeaker(string name, Sprite portrait);
/// <summary>
/// 刷新当前对话文本内容。
/// </summary>
void SetContent(string text, bool isTyping);
/// <summary>
/// 生成并显示当前节点选项列表。
/// </summary>
void ShowChoices(IReadOnlyList<DialogueChoiceViewModel> choices);
/// <summary>
/// 销毁并清空当前选项按钮。
/// </summary>
void ClearChoices();
}
