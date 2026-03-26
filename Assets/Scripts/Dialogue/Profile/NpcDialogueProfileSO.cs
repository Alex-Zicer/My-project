using System.Collections.Generic;
using UnityEngine;

// NPC 对话配置：一个 NPC 绑定一个 Profile，由规则决定每次交互播放什么。
[CreateAssetMenu(fileName = "NpcDialogueProfile", menuName = "Data/Dialogue/NpcDialogueProfile")]
public class NpcDialogueProfileSO : ScriptableObject
{
    // Profile 标识（用于进度分组；为空时回退到资产名）。
    public string profileId = "npc_profile";

    // 规则列表（按 priority 从高到低匹配）。
    public List<NpcDialogueRule> rules = new List<NpcDialogueRule>();

    // 没有规则命中时的默认对话引用。
    public DialogueReference defaultDialogueReference = new DialogueReference();
    // 默认对话的重复策略。
    public DialogueRepeatPolicy defaultRepeatPolicy = DialogueRepeatPolicy.Repeatable;

    // 返回稳定可用的 profileId（未配置时回退到资产名）。
    public string GetProfileId()
    {
        return string.IsNullOrWhiteSpace(profileId) ? name : profileId;
    }
}
