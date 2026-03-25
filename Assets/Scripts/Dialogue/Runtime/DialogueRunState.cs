// 对话运行状态机：
// DialogueService 通过该枚举控制“输入行为”和“UI 呈现行为”。
public enum DialogueRunState
{
    // 空闲态：当前没有对话在运行。
    Idle,
    // 打字态：正文正在逐字显示，下一次“继续输入”会变成“立即补全文字”。
    Typing,
    // 等待继续：整句显示完，等待玩家按键推进到下一节点。
    WaitingNext,
    // 等待选项：有分支选项时进入，必须等待玩家点击选项。
    WaitingChoice
}
