// 瀵硅瘽杩涘害瀛樺偍鎺ュ彛锛氭娊璞♀€滈娆?閲嶅鏄惁宸叉挱鏀锯€濈殑璇诲啓琛屼负銆?
public interface IDialogueProgressStore
{
    // 鏄惁宸茬粡鎾斁杩囪瑙勫垯鐨勯娆″璇濄€?
/// <summary>
/// HasPlayedFirst。
/// </summary>
/// <param name="npcId">参数。</param>
/// <param name="profileId">参数。</param>
/// <param name="ruleId">参数。</param>
bool HasPlayedFirst(string npcId, string profileId, string ruleId);
    // 鏄惁宸茬粡鎾斁杩囪瑙勫垯鐨勯噸澶嶅璇濄€?
/// <summary>
/// HasPlayedRepeat。
/// </summary>
/// <param name="npcId">参数。</param>
/// <param name="profileId">参数。</param>
/// <param name="ruleId">参数。</param>
bool HasPlayedRepeat(string npcId, string profileId, string ruleId);

    // 鏍囪璇ヨ鍒欓娆″璇濆凡鎾斁銆?
/// <summary>
/// MarkPlayedFirst。
/// </summary>
/// <param name="npcId">参数。</param>
/// <param name="profileId">参数。</param>
/// <param name="ruleId">参数。</param>
void MarkPlayedFirst(string npcId, string profileId, string ruleId);
    // 鏍囪璇ヨ鍒欓噸澶嶅璇濆凡鎾斁銆?
/// <summary>
/// MarkPlayedRepeat。
/// </summary>
/// <param name="npcId">参数。</param>
/// <param name="profileId">参数。</param>
/// <param name="ruleId">参数。</param>
void MarkPlayedRepeat(string npcId, string profileId, string ruleId);

    // 娓呯悊鏌愪釜 NPC 鐨勮繘搴︺€?
/// <summary>
/// ResetNpc。
/// </summary>
/// <param name="npcId">参数。</param>
void ResetNpc(string npcId);
    // 娓呯悊鍏ㄩ儴杩涘害銆?
/// <summary>
/// ResetAll。
/// </summary>
void ResetAll();
}
