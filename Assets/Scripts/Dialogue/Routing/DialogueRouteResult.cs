using System.Collections.Generic;

/// <summary>
/// 路由结果数据：记录命中规则、入口节点与回写项。
/// </summary>
public class DialogueRouteResult
{
    public string NpcId { get; private set; }
    public string ProfileId { get; private set; }
    public string RuleId { get; private set; }
    public DialogueRoutePhase Phase { get; private set; }
    public DialogueReference DialogueReference { get; private set; }
    public string StartNodeId { get; private set; }
    public IReadOnlyList<DialogueStateMutation> CompletionMutations { get; private set; }

    // IsValid 标识。
    public bool IsValid => DialogueReference != null;

    /// <summary>
    /// 创建一条完整的路由结果记录。
    /// </summary>
    public static DialogueRouteResult Create(
        string npcId,
        string profileId,
        string ruleId,
        DialogueRoutePhase phase,
        DialogueReference reference,
        string startNodeId,
        IReadOnlyList<DialogueStateMutation> completionMutations)
    {
        return new DialogueRouteResult
        {
            NpcId = npcId ?? string.Empty,
            ProfileId = profileId ?? string.Empty,
            RuleId = ruleId ?? string.Empty,
            Phase = phase,
            DialogueReference = reference,
            StartNodeId = startNodeId,
            CompletionMutations = completionMutations ?? System.Array.Empty<DialogueStateMutation>()
        };
    }
}
