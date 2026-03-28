using System.Collections.Generic;

// 璺敱缁撴灉锛氫繚瀛樷€滆繖娆￠€変腑浜嗗摢鏉¤鍒欍€佸摢娈靛璇濄€佺粨鏉熷悗瑕佸啓鍥炰粈涔堢姸鎬佲€濄€?
public class DialogueRouteResult
{
    // 褰撳墠浜や簰鐨?NPC 鏍囪瘑銆?
    public string NpcId { get; private set; }
    // 鍙備笌鍖归厤鐨?Profile 鏍囪瘑銆?
    public string ProfileId { get; private set; }
    // 鍛戒腑鐨勮鍒?ID锛堥粯璁ゅ璇濅細浣跨敤鍥哄畾榛樿 ID锛夈€?
    public string RuleId { get; private set; }
    // 鏈鍛戒腑鐨勯樁娈碉紙First/Repeat/Default锛夈€?
    public DialogueRoutePhase Phase { get; private set; }
    // 鏈€缁堥€変腑鐨勫璇濆紩鐢ㄣ€?
    public DialogueReference DialogueReference { get; private set; }
    // 瀵硅瘽缁撴潫鏃惰鎵ц鐨勭姸鎬佸啓鍥炲垪琛ㄣ€?
    public IReadOnlyList<DialogueStateMutation> CompletionMutations { get; private set; }

    // 鍒ゆ柇璇ョ粨鏋滄槸鍚﹀寘鍚彲鎾斁瀵硅瘽寮曠敤銆?
    public bool IsValid => DialogueReference != null;

    // 鐢熸垚瀹屾暣璺敱缁撴灉锛屼緵 DialogueService 鍚姩涓庣粨鏉熷洖鍐欎娇鐢ㄣ€?
    public static DialogueRouteResult Create(
        string npcId,
        string profileId,
        string ruleId,
        DialogueRoutePhase phase,
        DialogueReference reference,
        IReadOnlyList<DialogueStateMutation> completionMutations)
    {
        return new DialogueRouteResult
        {
            NpcId = npcId ?? string.Empty,
            ProfileId = profileId ?? string.Empty,
            RuleId = ruleId ?? string.Empty,
            Phase = phase,
            DialogueReference = reference,
            CompletionMutations = completionMutations ?? System.Array.Empty<DialogueStateMutation>()
        };
    }
}
