using System;
using System.Collections.Generic;
using UnityEngine;

// 对话展示接口（运行层与 UI 层解耦边界）：
// DialogueService 只依赖该接口，不直接依赖 TMP、Button、具体页面实现。
public interface IDialogueView
{
    // 用户请求“继续下一步”（按键点击等）时触发。
    event Action OnNextRequested;
    // 用户选择分支选项时触发，参数为选项索引。
    event Action<int> OnChoiceSelected;

    // 打开对话界面。
    void Open();
    // 关闭对话界面。
    void Close();
    // 设置说话人信息。
    void SetSpeaker(string name, Sprite portrait);
    // 刷新正文文本；isTyping 表示是否处于打字机过程。
    void SetContent(string text, bool isTyping);
    // 显示可选分支列表。
    void ShowChoices(IReadOnlyList<DialogueChoiceViewModel> choices);
    // 清空当前分支按钮。
    void ClearChoices();
}
