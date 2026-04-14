using System.Collections.Generic;

public class DialogueMemoryProgressStore : IDialogueProgressStore
{
    // 已播放“首次分支”的规则键集合。
    private readonly HashSet<string> _firstPlayed = new HashSet<string>();

    // 已播放“重复分支”的规则键集合。
    private readonly HashSet<string> _repeatPlayed = new HashSet<string>();

    /// <summary>
    /// 判断规则首次对话是否已播放。
    /// </summary>
    public bool HasPlayedFirst(string npcId, string profileId, string ruleId)
    {
        return _firstPlayed.Contains(BuildKey(npcId, profileId, ruleId));
    }

    /// <summary>
    /// 判断规则重复对话是否已播放。
    /// </summary>
    public bool HasPlayedRepeat(string npcId, string profileId, string ruleId)
    {
        return _repeatPlayed.Contains(BuildKey(npcId, profileId, ruleId));
    }

    /// <summary>
    /// 标记规则首次对话已播放。
    /// </summary>
    public void MarkPlayedFirst(string npcId, string profileId, string ruleId)
    {
        _firstPlayed.Add(BuildKey(npcId, profileId, ruleId));
    }

    /// <summary>
    /// 标记规则重复对话已播放。
    /// </summary>
    public void MarkPlayedRepeat(string npcId, string profileId, string ruleId)
    {
        _repeatPlayed.Add(BuildKey(npcId, profileId, ruleId));
    }

    /// <summary>
    /// 清空指定 NPC 的对话进度记录。
    /// </summary>
    public void ResetNpc(string npcId)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(npcId)) return;
        string prefix = npcId + "::";
        RemoveByPrefix(_firstPlayed, prefix);
        RemoveByPrefix(_repeatPlayed, prefix);
    }

    /// <summary>
    /// 清空全部 NPC 的对话进度记录。
    /// </summary>
    public void ResetAll()
    {
        _firstPlayed.Clear();
        _repeatPlayed.Clear();
    }

    /// <summary>
    /// 拼接 NPC、Profile 与规则的进度键。
    /// </summary>
    private static string BuildKey(string npcId, string profileId, string ruleId)
    {
        string safeNpc = string.IsNullOrWhiteSpace(npcId) ? "__npc__" : npcId;
        string safeProfile = string.IsNullOrWhiteSpace(profileId) ? "__profile__" : profileId;
        string safeRule = string.IsNullOrWhiteSpace(ruleId) ? "__rule__" : ruleId;
        return safeNpc + "::" + safeProfile + "::" + safeRule;
    }

    /// <summary>
    /// 按前缀批量移除匹配的进度键。
    /// </summary>
    private static void RemoveByPrefix(HashSet<string> set, string prefix)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (set == null || set.Count == 0) return;

        List<string> toRemove = null;
        foreach (string key in set)
        {
            if (!key.StartsWith(prefix)) continue;
            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (toRemove == null) toRemove = new List<string>();
            toRemove.Add(key);
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (toRemove == null) return;
        // 遍历集合并逐项处理当前业务。
        for (int i = 0; i < toRemove.Count; i++)
        {
            set.Remove(toRemove[i]);
        }
    }
}
