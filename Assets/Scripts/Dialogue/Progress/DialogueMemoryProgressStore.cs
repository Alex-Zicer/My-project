using System.Collections.Generic;

// 鍐呭瓨鐗堣繘搴﹀瓨鍌細褰撳墠浼氳瘽鏈夋晥锛岄€傚悎鍏堣仈閫氭祦绋嬶紱鍚庣画鍙浛鎹负鎸佷箙鍖栧瓨妗ｅ疄鐜般€?
public class DialogueMemoryProgressStore : IDialogueProgressStore
{
    // 棣栨鎾斁璁板綍闆嗗悎銆?
    private readonly HashSet<string> _firstPlayed = new HashSet<string>();
    // 閲嶅鎾斁璁板綍闆嗗悎銆?
    private readonly HashSet<string> _repeatPlayed = new HashSet<string>();

    // 鍒ゆ柇鏌愯鍒欑殑棣栨瀵硅瘽鏄惁宸茬粡鎾斁杩囥€?
    /// <summary>
    /// HasPlayedFirst。
    /// </summary>
    /// <param name="npcId">参数。</param>
    /// <param name="profileId">参数。</param>
    /// <param name="ruleId">参数。</param>
    public bool HasPlayedFirst(string npcId, string profileId, string ruleId)
    {
        return _firstPlayed.Contains(BuildKey(npcId, profileId, ruleId));
    }

    // 鍒ゆ柇鏌愯鍒欑殑閲嶅瀵硅瘽鏄惁宸茬粡鎾斁杩囥€?
    /// <summary>
    /// HasPlayedRepeat。
    /// </summary>
    /// <param name="npcId">参数。</param>
    /// <param name="profileId">参数。</param>
    /// <param name="ruleId">参数。</param>
    public bool HasPlayedRepeat(string npcId, string profileId, string ruleId)
    {
        return _repeatPlayed.Contains(BuildKey(npcId, profileId, ruleId));
    }

    // 璁板綍鏌愯鍒欑殑棣栨瀵硅瘽宸叉挱鏀俱€?
    /// <summary>
    /// MarkPlayedFirst。
    /// </summary>
    /// <param name="npcId">参数。</param>
    /// <param name="profileId">参数。</param>
    /// <param name="ruleId">参数。</param>
    public void MarkPlayedFirst(string npcId, string profileId, string ruleId)
    {
        _firstPlayed.Add(BuildKey(npcId, profileId, ruleId));
    }

    // 璁板綍鏌愯鍒欑殑閲嶅瀵硅瘽宸叉挱鏀俱€?
    /// <summary>
    /// MarkPlayedRepeat。
    /// </summary>
    /// <param name="npcId">参数。</param>
    /// <param name="profileId">参数。</param>
    /// <param name="ruleId">参数。</param>
    public void MarkPlayedRepeat(string npcId, string profileId, string ruleId)
    {
        _repeatPlayed.Add(BuildKey(npcId, profileId, ruleId));
    }

    // 娓呯悊鎸囧畾 NPC 鐨勫叏閮ㄥ璇濊繘搴︺€?
    /// <summary>
    /// ResetNpc。
    /// </summary>
    /// <param name="npcId">参数。</param>
    public void ResetNpc(string npcId)
    {
        if (string.IsNullOrWhiteSpace(npcId)) return;
        string prefix = npcId + "::";
        RemoveByPrefix(_firstPlayed, prefix);
        RemoveByPrefix(_repeatPlayed, prefix);
    }

    // 娓呯悊鍏ㄩ儴 NPC 鐨勫璇濊繘搴︺€?
    /// <summary>
    /// ResetAll。
    /// </summary>
    public void ResetAll()
    {
        _firstPlayed.Clear();
        _repeatPlayed.Clear();
    }

    // 鎶?npc/profile/rule 缁勫悎鎴愮ǔ瀹?key锛岀敤浜?HashSet 瀛樺彇銆?
    /// <summary>
    /// BuildKey。
    /// </summary>
    /// <param name="npcId">参数。</param>
    /// <param name="profileId">参数。</param>
    /// <param name="ruleId">参数。</param>
    private static string BuildKey(string npcId, string profileId, string ruleId)
    {
        string safeNpc = string.IsNullOrWhiteSpace(npcId) ? "__npc__" : npcId;
        string safeProfile = string.IsNullOrWhiteSpace(profileId) ? "__profile__" : profileId;
        string safeRule = string.IsNullOrWhiteSpace(ruleId) ? "__rule__" : ruleId;
        return safeNpc + "::" + safeProfile + "::" + safeRule;
    }

    // 鍒犻櫎鎸囧畾鍓嶇紑鐨勫叏閮ㄨ褰曪紙鐢ㄤ簬鎸?NPC 娓呯悊杩涘害锛夈€?
    /// <summary>
    /// RemoveByPrefix。
    /// </summary>
    /// <param name="set">参数。</param>
    /// <param name="prefix">参数。</param>
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
