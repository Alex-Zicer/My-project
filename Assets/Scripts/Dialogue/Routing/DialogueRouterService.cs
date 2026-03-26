using System.Collections.Generic;
using UnityEngine;

// 对话路由服务：根据 NPC/Profile/剧情状态解析本次应播放的对话，并在结束时回写进度与状态。
public class DialogueRouterService : MonoBehaviour
{
    // 排序辅助结构：保存规则与原始索引（同优先级时按原顺序稳定排序）。
    private sealed class RuleEntry
    {
        // 规则对象。
        public NpcDialogueRule Rule;
        // 规则在原列表中的索引。
        public int Index;
    }

    // 空状态读取器：当未接入状态系统时，提供“全失败”的安全读取。
    private sealed class NullGameStateReader : IDialogueGameStateReader
    {
        public static readonly NullGameStateReader Instance = new NullGameStateReader();

        public bool HasKey(string key) => false;
        public bool TryGetBool(string key, out bool value) { value = false; return false; }
        public bool TryGetInt(string key, out int value) { value = 0; return false; }
        public bool TryGetString(string key, out string value) { value = string.Empty; return false; }
    }

    private static DialogueRouterService _instance;

    // 对话进度存储（默认内存实现，可注入替换）。
    private IDialogueProgressStore _progressStore = new DialogueMemoryProgressStore();
    // 规则排序缓冲，减少运行时临时分配。
    private readonly List<RuleEntry> _ruleBuffer = new List<RuleEntry>();

    public static bool HasInstance => _instance != null;

    public static DialogueRouterService Instance
    {
        get
        {
            if (_instance == null)
            {
                CreateInstance();
            }
            return _instance;
        }
    }

    // 延迟创建路由服务实例，避免场景手动摆放依赖。
    private static void CreateInstance()
    {
        var go = new GameObject("DialogueRouterService");
        _instance = go.AddComponent<DialogueRouterService>();
        DontDestroyOnLoad(go);
    }

    // 确保场景中只有一个路由服务实例。
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

    // 对象销毁时清理静态实例引用。
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    // 注入自定义进度存储（例如存档持久化实现）。
    public void SetProgressStore(IDialogueProgressStore progressStore)
    {
        if (progressStore == null) return;
        _progressStore = progressStore;
    }

    // 依据 NPC + Profile + 当前状态挑选本次应该播放的对话引用。
    public bool TryResolve(
        string npcId,
        NpcDialogueProfileSO profile,
        out DialogueRouteResult routeResult,
        out string error)
    {
        routeResult = null;
        error = string.Empty;

        if (string.IsNullOrWhiteSpace(npcId))
        {
            error = "npcId 为空。";
            return false;
        }

        if (profile == null)
        {
            error = $"NPC '{npcId}' 未配置 NpcDialogueProfile。";
            return false;
        }

        string profileId = profile.GetProfileId();
        IDialogueGameStateReader stateReader = GetStateReader();
        BuildSortedRules(profile.rules);

        for (int i = 0; i < _ruleBuffer.Count; i++)
        {
            RuleEntry entry = _ruleBuffer[i];
            NpcDialogueRule rule = entry.Rule;
            if (rule == null || !rule.enabled) continue;
            if (!rule.IsMatch(stateReader)) continue;

            string ruleId = ResolveRuleId(rule, entry.Index);
            if (TryResolveFromRule(npcId, profileId, ruleId, rule, out routeResult))
            {
                return true;
            }
        }

        if (TryResolveDefault(npcId, profileId, profile, out routeResult))
        {
            return true;
        }

        error = $"NPC '{npcId}' 未命中可播放对话，请检查 Profile '{profileId}' 的规则配置。";
        return false;
    }

    // 在对话结束后写回“首次/重复进度”并应用状态变更。
    public void NotifyDialogueCompleted(DialogueRouteResult routeResult)
    {
        if (routeResult == null || !routeResult.IsValid) return;

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

        ApplyMutations(routeResult.CompletionMutations);
    }

    // 对单条规则执行“首次/重复”分流，返回可播放的对话引用。
    private bool TryResolveFromRule(
        string npcId,
        string profileId,
        string ruleId,
        NpcDialogueRule rule,
        out DialogueRouteResult routeResult)
    {
        routeResult = null;
        bool firstPlayed = _progressStore.HasPlayedFirst(npcId, profileId, ruleId);

        if (!firstPlayed && IsReferenceConfigured(rule.firstDialogueReference))
        {
            routeResult = DialogueRouteResult.Create(
                npcId,
                profileId,
                ruleId,
                DialogueRoutePhase.First,
                rule.firstDialogueReference,
                rule.onFirstCompleted);
            return true;
        }

        if (IsReferenceConfigured(rule.repeatDialogueReference))
        {
            bool repeatPlayed = _progressStore.HasPlayedRepeat(npcId, profileId, ruleId);
            if (rule.repeatRepeatPolicy == DialogueRepeatPolicy.Once && repeatPlayed)
            {
                return false;
            }

            routeResult = DialogueRouteResult.Create(
                npcId,
                profileId,
                ruleId,
                DialogueRoutePhase.Repeat,
                rule.repeatDialogueReference,
                rule.onRepeatCompleted);
            return true;
        }

        if (firstPlayed &&
            rule.firstRepeatPolicy == DialogueRepeatPolicy.Repeatable &&
            IsReferenceConfigured(rule.firstDialogueReference))
        {
            routeResult = DialogueRouteResult.Create(
                npcId,
                profileId,
                ruleId,
                DialogueRoutePhase.Repeat,
                rule.firstDialogueReference,
                rule.onRepeatCompleted != null && rule.onRepeatCompleted.Count > 0
                    ? rule.onRepeatCompleted
                    : rule.onFirstCompleted);
            return true;
        }

        return false;
    }

    // 当没有规则可用时，尝试使用 Profile 的默认对话。
    private bool TryResolveDefault(
        string npcId,
        string profileId,
        NpcDialogueProfileSO profile,
        out DialogueRouteResult routeResult)
    {
        routeResult = null;
        if (profile == null) return false;
        if (!IsReferenceConfigured(profile.defaultDialogueReference)) return false;

        const string defaultRuleId = "__default__";
        bool firstPlayed = _progressStore.HasPlayedFirst(npcId, profileId, defaultRuleId);
        if (firstPlayed && profile.defaultRepeatPolicy == DialogueRepeatPolicy.Once)
        {
            return false;
        }

        routeResult = DialogueRouteResult.Create(
            npcId,
            profileId,
            defaultRuleId,
            firstPlayed ? DialogueRoutePhase.Repeat : DialogueRoutePhase.Default,
            profile.defaultDialogueReference,
            System.Array.Empty<DialogueStateMutation>());
        return true;
    }

    // 执行路由结果里携带的状态写回列表。
    private void ApplyMutations(IReadOnlyList<DialogueStateMutation> mutations)
    {
        if (mutations == null || mutations.Count == 0) return;

        IDialogueGameStateWriter writer = DialogueGameStateService.HasInstance
            ? DialogueGameStateService.Instance
            : null;

        if (writer == null)
        {
            Debug.LogWarning("[DialogueRouterService] 有状态写回配置，但未找到 DialogueGameStateService。");
            return;
        }

        for (int i = 0; i < mutations.Count; i++)
        {
            DialogueStateMutation mutation = mutations[i];
            mutation?.Apply(writer);
        }
    }

    // 获取状态读取器；未接入状态服务时回退空读取器。
    private IDialogueGameStateReader GetStateReader()
    {
        if (DialogueGameStateService.HasInstance) return DialogueGameStateService.Instance;
        return NullGameStateReader.Instance;
    }

    // 将规则按优先级排序；同优先级保持配置顺序。
    private void BuildSortedRules(List<NpcDialogueRule> rules)
    {
        _ruleBuffer.Clear();
        if (rules == null || rules.Count == 0) return;

        for (int i = 0; i < rules.Count; i++)
        {
            NpcDialogueRule rule = rules[i];
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

    // 生成稳定规则 ID（未配置时使用 rule_索引）。
    private static string ResolveRuleId(NpcDialogueRule rule, int index)
    {
        if (rule != null && !string.IsNullOrWhiteSpace(rule.ruleId))
        {
            return rule.ruleId.Trim();
        }
        return "rule_" + index;
    }

    // 判断对话引用是否至少配置了一个可加载来源。
    private static bool IsReferenceConfigured(DialogueReference reference)
    {
        if (reference == null) return false;
        if (reference.primarySO != null) return true;
        if (reference.fallbackSO != null) return true;
        return !string.IsNullOrWhiteSpace(reference.keyOrPath);
    }
}
