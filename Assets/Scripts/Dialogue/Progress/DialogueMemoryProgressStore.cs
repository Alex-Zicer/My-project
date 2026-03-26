using System.Collections.Generic;

// 内存版进度存储：当前会话有效，适合先联通流程；后续可替换为持久化存档实现。
public class DialogueMemoryProgressStore : IDialogueProgressStore
{
    // 首次播放记录集合。
    private readonly HashSet<string> _firstPlayed = new HashSet<string>();
    // 重复播放记录集合。
    private readonly HashSet<string> _repeatPlayed = new HashSet<string>();

    // 判断某规则的首次对话是否已经播放过。
    public bool HasPlayedFirst(string npcId, string profileId, string ruleId)
    {
        return _firstPlayed.Contains(BuildKey(npcId, profileId, ruleId));
    }

    // 判断某规则的重复对话是否已经播放过。
    public bool HasPlayedRepeat(string npcId, string profileId, string ruleId)
    {
        return _repeatPlayed.Contains(BuildKey(npcId, profileId, ruleId));
    }

    // 记录某规则的首次对话已播放。
    public void MarkPlayedFirst(string npcId, string profileId, string ruleId)
    {
        _firstPlayed.Add(BuildKey(npcId, profileId, ruleId));
    }

    // 记录某规则的重复对话已播放。
    public void MarkPlayedRepeat(string npcId, string profileId, string ruleId)
    {
        _repeatPlayed.Add(BuildKey(npcId, profileId, ruleId));
    }

    // 清理指定 NPC 的全部对话进度。
    public void ResetNpc(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId)) return;
        string prefix = npcId + "::";
        RemoveByPrefix(_firstPlayed, prefix);
        RemoveByPrefix(_repeatPlayed, prefix);
    }

    // 清理全部 NPC 的对话进度。
    public void ResetAll()
    {
        _firstPlayed.Clear();
        _repeatPlayed.Clear();
    }

    // 把 npc/profile/rule 组合成稳定 key，用于 HashSet 存取。
    private static string BuildKey(string npcId, string profileId, string ruleId)
    {
        string safeNpc = string.IsNullOrWhiteSpace(npcId) ? "__npc__" : npcId;
        string safeProfile = string.IsNullOrWhiteSpace(profileId) ? "__profile__" : profileId;
        string safeRule = string.IsNullOrWhiteSpace(ruleId) ? "__rule__" : ruleId;
        return safeNpc + "::" + safeProfile + "::" + safeRule;
    }

    // 删除指定前缀的全部记录（用于按 NPC 清理进度）。
    private static void RemoveByPrefix(HashSet<string> set, string prefix)
    {
        if (set == null || set.Count == 0) return;

        List<string> toRemove = null;
        foreach (string key in set)
        {
            if (!key.StartsWith(prefix)) continue;
            if (toRemove == null) toRemove = new List<string>();
            toRemove.Add(key);
        }

        if (toRemove == null) return;
        for (int i = 0; i < toRemove.Count; i++)
        {
            set.Remove(toRemove[i]);
        }
    }
}
