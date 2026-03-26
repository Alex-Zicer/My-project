// 对话进度存储接口：抽象“首次/重复是否已播放”的读写行为。
public interface IDialogueProgressStore
{
    // 是否已经播放过该规则的首次对话。
    bool HasPlayedFirst(string npcId, string profileId, string ruleId);
    // 是否已经播放过该规则的重复对话。
    bool HasPlayedRepeat(string npcId, string profileId, string ruleId);

    // 标记该规则首次对话已播放。
    void MarkPlayedFirst(string npcId, string profileId, string ruleId);
    // 标记该规则重复对话已播放。
    void MarkPlayedRepeat(string npcId, string profileId, string ruleId);

    // 清理某个 NPC 的进度。
    void ResetNpc(string npcId);
    // 清理全部进度。
    void ResetAll();
}
