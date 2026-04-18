using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话路由服务：根据 NPC、Profile 与当前剧情布尔状态， 解析本次交互应播放的对话入口（首次/重复），并在对话完成后写回进度与状态。
/// </summary>
public class DialogueRouterService : MonoBehaviour
{
    /// <summary>
    /// 规则排序缓存项：保存规则本体与其原始索引。 原始索引用于在同优先级下保持稳定顺序。
    /// </summary>
    private sealed class RuleEntry
    {
        // Rule 运行时字段。
        public NpcDialogueRule Rule;
        // Index 运行时字段。
        public int Index;
    }

    /// <summary>
    /// 空状态读取器：当未接入状态服务时，提供安全的默认读行为。
    /// </summary>
    private sealed class NullGameStateReader : IDialogueGameStateReader
    {
        // 空读取器单例，避免重复分配。
        public static readonly NullGameStateReader Instance = new NullGameStateReader();

        public bool HasKey(string key) => false;
        public bool TryGetBool(string key, out bool value) { value = false; return false; }
    }

    // 单例实例引用。
    private static DialogueRouterService _instance;

    // 对话进度存储（默认内存实现，可被外部注入替换）。
    private IDialogueProgressStore _progressStore = new DialogueMemoryProgressStore();

    // 规则排序缓存，避免每次解析产生临时集合。
    private readonly List<RuleEntry> _ruleBuffer = new List<RuleEntry>();

    /// <summary>
    /// 当前场景中是否已经存在路由服务实例。
    /// </summary>
    public static bool HasInstance => _instance != null;

    /// <summary>
    /// 单例访问入口；若不存在则自动创建。
    /// </summary>
    public static DialogueRouterService Instance
    {
        get
        {
            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (_instance == null)
            {
                CreateInstance();
            }
            return _instance;
        }
    }

    /// <summary>
    /// 延迟创建路由服务单例并跨场景保留。
    /// </summary>
    private static void CreateInstance()
    {
        var go = new GameObject("DialogueRouterService");
        _instance = go.AddComponent<DialogueRouterService>();
        DontDestroyOnLoad(go);
    }

    /// <summary>
    /// 保证场景中只保留一个路由服务实例。
    /// </summary>
    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }

        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 销毁时清理单例引用。
    /// </summary>
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// 注入自定义进度存储实现（例如持久化存档）。
    /// </summary>
    public void SetProgressStore(IDialogueProgressStore progressStore)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (progressStore == null) return;
        _progressStore = progressStore;
    }

    /// <summary>
    /// 根据 NPC 与 Profile 解析本次应播放的对话路由结果。 解析顺序：按优先级规则匹配，若未命中则尝试默认对话。
    /// </summary>
    public bool TryResolve(
        string npcId,
        NpcDialogueProfileSO profile,
        out DialogueRouteResult routeResult,
        out string error)
    {
        routeResult = null;
        error = string.Empty;

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(npcId))
        {
            error = "npcId 为空。";
            return false;
        }

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (profile == null)
        {
            error = $"NPC '{npcId}' 未配置 NpcDialogueProfile。";
            return false;
        }

        string profileId = profile.GetProfileId();
        IDialogueGameStateReader stateReader = GetStateReader();
        BuildSortedRules(profile.rules);

        // 按优先级从高到低遍历，命中第一条可用规则即返回。
        for (int i = 0; i < _ruleBuffer.Count; i++)
        {
            RuleEntry entry = _ruleBuffer[i];
            NpcDialogueRule rule = entry.Rule;
            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (rule == null || !rule.enabled) continue;
            if (!rule.IsMatch(stateReader)) continue;

            string ruleId = ResolveRuleId(rule, entry.Index);
            if (TryResolveFromRule(npcId, profileId, ruleId, rule, out routeResult))
            {
                return true;
            }
        }

        // 未命中任何规则时，尝试默认对话配置。
        if (TryResolveDefault(npcId, profileId, profile, out routeResult))
        {
            return true;
        }

        error = $"NPC '{npcId}' 未命中可播放对话，请检查 Profile '{profileId}' 的规则配置。";
        return false;
    }

    /// <summary>
    /// 对话结束后写回进度并应用状态变更。
    /// </summary>
    public void NotifyDialogueCompleted(DialogueRouteResult routeResult)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (routeResult == null || !routeResult.IsValid) return;

        // 记录首次/重复播放进度，供下次路由判断。
        switch (routeResult.Phase)
        {
            case DialogueRoutePhase.First:
            case DialogueRoutePhase.Default:
                _progressStore.MarkPlayedFirst(routeResult.NpcId, routeResult.ProfileId, routeResult.RuleId);
                break;
            case DialogueRoutePhase.Repeat:
                _progressStore.MarkPlayedRepeat(routeResult.NpcId, routeResult.ProfileId, routeResult.RuleId);
                break;
        }

        // 应用规则配置的状态写回项。
        ApplyMutations(routeResult.CompletionMutations);
    }

    /// <summary>
    /// 依据单条规则构造路由结果。 首次播放使用 firstStartNodeId，非首次播放使用 repeatStartNodeId。
    /// </summary>
    private bool TryResolveFromRule(
        string npcId,
        string profileId,
        string ruleId,
        NpcDialogueRule rule,
        out DialogueRouteResult routeResult)
    {
        routeResult = null;

        if (!IsReferenceConfigured(rule.dialogueReference))
        {
            return false;
        }

        // 仅以“是否播过首次”区分当前阶段。
        bool isFirstPlay = !_progressStore.HasPlayedFirst(npcId, profileId, ruleId);

        // 根据阶段选择入口节点：首次 -> firstStartNodeId，重复 -> repeatStartNodeId。
        string startNodeId = isFirstPlay
            ? rule.dialogueReference.firstStartNodeId
            : rule.dialogueReference.repeatStartNodeId;

        // 若重复入口未配置，则回退到首次入口。
        if (string.IsNullOrWhiteSpace(startNodeId))
        {
            startNodeId = rule.dialogueReference.firstStartNodeId;
        }

        routeResult = DialogueRouteResult.Create(
            npcId,
            profileId,
            ruleId,
            isFirstPlay ? DialogueRoutePhase.First : DialogueRoutePhase.Repeat,
            rule.dialogueReference,
            startNodeId,
            rule.onCompleted);

        return true;
    }

    /// <summary>
    /// 当规则都未命中时，尝试解析 Profile 的默认对话。
    /// </summary>
    private bool TryResolveDefault(
        string npcId,
        string profileId,
        NpcDialogueProfileSO profile,
        out DialogueRouteResult routeResult)
    {
        routeResult = null;
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (profile == null) return false;
        if (!IsReferenceConfigured(profile.defaultDialogueReference)) return false;

        const string defaultRuleId = "__default__";
        bool firstPlayed = _progressStore.HasPlayedFirst(npcId, profileId, defaultRuleId);

        // 默认对话同样遵循首次/重复双入口。
        string startNodeId = !firstPlayed
            ? profile.defaultDialogueReference.firstStartNodeId
            : profile.defaultDialogueReference.repeatStartNodeId;

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (string.IsNullOrWhiteSpace(startNodeId))
        {
            startNodeId = profile.defaultDialogueReference.firstStartNodeId;
        }

        routeResult = DialogueRouteResult.Create(
            npcId,
            profileId,
            defaultRuleId,
            firstPlayed ? DialogueRoutePhase.Repeat : DialogueRoutePhase.Default,
            profile.defaultDialogueReference,
            startNodeId,
            System.Array.Empty<DialogueStateMutation>());

        return true;
    }

    /// <summary>
    /// 执行路由结果携带的状态写回列表。
    /// </summary>
    private void ApplyMutations(IReadOnlyList<DialogueStateMutation> mutations)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (mutations == null || mutations.Count == 0) return;

        // 按需获取状态服务实例，保证写回链路可用。
        DialogueGameStateService gameStateService = DialogueGameStateService.Instance;

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (gameStateService == null)
        {
            Debug.LogWarning("[DialogueRouterService] 有状态写回配置，但状态服务实例为空，写回已跳过。");
            return;
        }

        // 遍历集合并逐项处理当前业务。
        for (int i = 0; i < mutations.Count; i++)
        {
            DialogueStateMutation mutation = mutations[i];
            mutation?.Apply(gameStateService);
        }
    }

    /// <summary>
    /// 获取状态读取器；未接入状态服务时回退到空读取器。
    /// </summary>
    private IDialogueGameStateReader GetStateReader()
    {
        // 按需获取状态服务实例，保证规则条件读取与写回一致。
        return DialogueGameStateService.Instance;
    }

    /// <summary>
    /// 按优先级对规则进行稳定排序。 同优先级下保持配置列表中的原顺序。
    /// </summary>
    private void BuildSortedRules(List<NpcDialogueRule> rules)
    {
        _ruleBuffer.Clear();
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (rules == null || rules.Count == 0) return;

        // 遍历集合并逐项处理当前业务。
        for (int i = 0; i < rules.Count; i++)
        {
            NpcDialogueRule rule = rules[i];
            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (rule == null) continue;
            _ruleBuffer.Add(new RuleEntry
            {
                Rule = rule,
                Index = i
            });
        }

        _ruleBuffer.Sort((a, b) =>
        {
            int p = b.Rule.priority.CompareTo(a.Rule.priority);
            if (p != 0) return p;
            return a.Index.CompareTo(b.Index);
        });
    }

    /// <summary>
    /// 解析规则 ID：优先使用配置的 ruleId，未配置时使用稳定回退 ID。
    /// </summary>
    private static string ResolveRuleId(NpcDialogueRule rule, int index)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (rule != null && !string.IsNullOrWhiteSpace(rule.ruleId))
        {
            return rule.ruleId.Trim();
        }
        return "rule_" + index;
    }

    /// <summary>
    /// 判断对话引用是否至少配置了一个可加载来源。
    /// </summary>
    private static bool IsReferenceConfigured(DialogueReference reference)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (reference == null) return false;
        if (reference.primarySO != null) return true;
        if (reference.fallbackSO != null) return true;
        return !string.IsNullOrWhiteSpace(reference.keyOrPath);
    }
}
