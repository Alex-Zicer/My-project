using System.Collections.Generic;

// 路由结果：保存“这次选中了哪条规则、哪段对话、结束后要写回什么状态”。
public class DialogueRouteResult
{
    // 当前交互的 NPC 标识。
    public string NpcId { get; private set; }
    // 参与匹配的 Profile 标识。
    public string ProfileId { get; private set; }
    // 命中的规则 ID（默认对话会使用固定默认 ID）。
    public string RuleId { get; private set; }
    // 本次命中的阶段（First/Repeat/Default）。
    public DialogueRoutePhase Phase { get; private set; }
    // 最终选中的对话引用。
    public DialogueReference DialogueReference { get; private set; }
    // 对话结束时要执行的状态写回列表。
    public IReadOnlyList<DialogueStateMutation> CompletionMutations { get; private set; }

    // 判断该结果是否包含可播放对话引用。
    public bool IsValid => DialogueReference != null;

    // 生成完整路由结果，供 DialogueService 启动与结束回写使用。
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
