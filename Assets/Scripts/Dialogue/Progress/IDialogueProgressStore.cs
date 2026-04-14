public interface IDialogueProgressStore
{
/// <summary>
/// 判断规则首次对话是否已播放。
/// </summary>
bool HasPlayedFirst(string npcId, string profileId, string ruleId);
/// <summary>
/// 判断规则重复对话是否已播放。
/// </summary>
bool HasPlayedRepeat(string npcId, string profileId, string ruleId);

/// <summary>
/// 标记规则首次对话已播放。
/// </summary>
void MarkPlayedFirst(string npcId, string profileId, string ruleId);
/// <summary>
/// 标记规则重复对话已播放。
/// </summary>
void MarkPlayedRepeat(string npcId, string profileId, string ruleId);

/// <summary>
/// 清空指定 NPC 的对话进度记录。
/// </summary>
void ResetNpc(string npcId);
/// <summary>
/// 清空全部 NPC 的对话进度记录。
/// </summary>
void ResetAll();
}
