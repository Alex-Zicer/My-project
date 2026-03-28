using System.Collections.Generic;
using UnityEngine;

// Resolves dialogue route by NPC profile and current game state.
public class DialogueRouterService : MonoBehaviour
{
    // Rule + original index pair for stable sorting.
    private sealed class RuleEntry
    {
        // Rule instance.
        public NpcDialogueRule Rule;

        // Original index in source list.
        public int Index;
    }

    // Null object for safe state reads when no state service is available.
    private sealed class NullGameStateReader : IDialogueGameStateReader
    {
        public static readonly NullGameStateReader Instance = new NullGameStateReader();

        /// <summary>
        /// Checks whether a key exists.
        /// </summary>
        /// <param name="key">State key.</param>
        /// <returns>Always false.</returns>
        public bool HasKey(string key)
        {
            return false;
        }

        /// <summary>
        /// Tries to read a bool value.
        /// </summary>
        /// <param name="key">State key.</param>
        /// <param name="value">Read output.</param>
        /// <returns>Always false.</returns>
        public bool TryGetBool(string key, out bool value)
        {
            value = false;
            return false;
        }

        /// <summary>
        /// Tries to read an int value.
        /// </summary>
        /// <param name="key">State key.</param>
        /// <param name="value">Read output.</param>
        /// <returns>Always false.</returns>
        public bool TryGetInt(string key, out int value)
        {
            value = 0;
            return false;
        }

        /// <summary>
        /// Tries to read a string value.
        /// </summary>
        /// <param name="key">State key.</param>
        /// <param name="value">Read output.</param>
        /// <returns>Always false.</returns>
        public bool TryGetString(string key, out string value)
        {
            value = string.Empty;
            return false;
        }
    }

    // Singleton instance.
    private static DialogueRouterService _instance;

    // Dialogue progress store.
    private IDialogueProgressStore _progressStore = new DialogueMemoryProgressStore();

    // Reused rule buffer to reduce allocations.
    private readonly List<RuleEntry> _ruleBuffer = new List<RuleEntry>();

    // Whether singleton already exists.
    public static bool HasInstance => _instance != null;

    // Singleton access point.
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

    /// <summary>
    /// Creates singleton instance on demand.
    /// </summary>
    private static void CreateInstance()
    {
        var go = new GameObject("DialogueRouterService");
        _instance = go.AddComponent<DialogueRouterService>();
        DontDestroyOnLoad(go);
    }

    /// <summary>
    /// Ensures singleton uniqueness.
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
    /// Clears singleton reference when destroyed.
    /// </summary>
    private void OnDestroy()
    {
        if (_instance == this)
        {
            _instance = null;
        }
    }

    /// <summary>
    /// Injects a custom dialogue progress store.
    /// </summary>
    /// <param name="progressStore">Store implementation.</param>
    public void SetProgressStore(IDialogueProgressStore progressStore)
    {
        if (progressStore == null)
        {
            return;
        }

        _progressStore = progressStore;
    }

    /// <summary>
    /// Resolves which dialogue should play for a given NPC and profile.
    /// </summary>
    /// <param name="npcId">NPC identifier.</param>
    /// <param name="profile">Dialogue profile.</param>
    /// <param name="routeResult">Route result output.</param>
    /// <param name="error">Error message output.</param>
    /// <returns>True when a playable route is found.</returns>
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
            error = "npcId is empty.";
            return false;
        }

        if (profile == null)
        {
            error = $"NPC '{npcId}' has no dialogue profile.";
            return false;
        }

        string profileId = profile.GetProfileId();
        IDialogueGameStateReader stateReader = GetStateReader();
        BuildSortedRules(profile.rules);

        // Walk sorted rules and return first playable route.
        for (int i = 0; i < _ruleBuffer.Count; i++)
        {
            RuleEntry entry = _ruleBuffer[i];
            NpcDialogueRule rule = entry.Rule;
            if (rule == null || !rule.enabled)
            {
                continue;
            }

            if (!rule.IsMatch(stateReader))
            {
                continue;
            }

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

        error = $"NPC '{npcId}' did not match any playable dialogue in profile '{profileId}'.";
        return false;
    }

    /// <summary>
    /// Applies progress and state mutations after a dialogue is completed.
    /// </summary>
    /// <param name="routeResult">Completed route result.</param>
    public void NotifyDialogueCompleted(DialogueRouteResult routeResult)
    {
        if (routeResult == null || !routeResult.IsValid)
        {
            return;
        }

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

    /// <summary>
    /// Resolves first/repeat route from one rule.
    /// </summary>
    /// <param name="npcId">NPC identifier.</param>
    /// <param name="profileId">Profile identifier.</param>
    /// <param name="ruleId">Rule identifier.</param>
    /// <param name="rule">Rule data.</param>
    /// <param name="routeResult">Route result output.</param>
    /// <returns>True when this rule yields a playable route.</returns>
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

    /// <summary>
    /// Resolves fallback default dialogue from profile.
    /// </summary>
    /// <param name="npcId">NPC identifier.</param>
    /// <param name="profileId">Profile identifier.</param>
    /// <param name="profile">Profile data.</param>
    /// <param name="routeResult">Route result output.</param>
    /// <returns>True when default dialogue is playable.</returns>
    private bool TryResolveDefault(
        string npcId,
        string profileId,
        NpcDialogueProfileSO profile,
        out DialogueRouteResult routeResult)
    {
        routeResult = null;
        if (profile == null)
        {
            return false;
        }

        if (!IsReferenceConfigured(profile.defaultDialogueReference))
        {
            return false;
        }

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

    /// <summary>
    /// Applies state mutations from route completion.
    /// </summary>
    /// <param name="mutations">Mutation list.</param>
    private void ApplyMutations(IReadOnlyList<DialogueStateMutation> mutations)
    {
        if (mutations == null || mutations.Count == 0)
        {
            return;
        }

        IDialogueGameStateWriter writer = DialogueGameStateService.HasInstance
            ? DialogueGameStateService.Instance
            : null;

        if (writer == null)
        {
            Debug.LogWarning("[DialogueRouterService] Mutations exist but DialogueGameStateService is missing.");
            return;
        }

        for (int i = 0; i < mutations.Count; i++)
        {
            DialogueStateMutation mutation = mutations[i];
            mutation?.Apply(writer);
        }
    }

    /// <summary>
    /// Gets state reader, with null-object fallback.
    /// </summary>
    /// <returns>State reader instance.</returns>
    private IDialogueGameStateReader GetStateReader()
    {
        if (DialogueGameStateService.HasInstance)
        {
            return DialogueGameStateService.Instance;
        }

        return NullGameStateReader.Instance;
    }

    /// <summary>
    /// Sorts rules by priority desc and source order asc.
    /// </summary>
    /// <param name="rules">Rule list from profile.</param>
    private void BuildSortedRules(List<NpcDialogueRule> rules)
    {
        _ruleBuffer.Clear();
        if (rules == null || rules.Count == 0)
        {
            return;
        }

        for (int i = 0; i < rules.Count; i++)
        {
            NpcDialogueRule rule = rules[i];
            if (rule == null)
            {
                continue;
            }

            _ruleBuffer.Add(new RuleEntry
            {
                Rule = rule,
                Index = i
            });
        }

        _ruleBuffer.Sort((a, b) =>
        {
            int p = b.Rule.priority.CompareTo(a.Rule.priority);
            if (p != 0)
            {
                return p;
            }

            return a.Index.CompareTo(b.Index);
        });
    }

    /// <summary>
    /// Resolves stable rule identifier.
    /// </summary>
    /// <param name="rule">Rule object.</param>
    /// <param name="index">Rule fallback index.</param>
    /// <returns>Rule id.</returns>
    private static string ResolveRuleId(NpcDialogueRule rule, int index)
    {
        if (rule != null && !string.IsNullOrWhiteSpace(rule.ruleId))
        {
            return rule.ruleId.Trim();
        }

        return "rule_" + index;
    }

    /// <summary>
    /// Checks whether at least one source is configured on the reference.
    /// </summary>
    /// <param name="reference">Dialogue reference.</param>
    /// <returns>True when source info is present.</returns>
    private static bool IsReferenceConfigured(DialogueReference reference)
    {
        if (reference == null)
        {
            return false;
        }

        if (reference.primarySO != null)
        {
            return true;
        }

        if (reference.fallbackSO != null)
        {
            return true;
        }

        return !string.IsNullOrWhiteSpace(reference.keyOrPath);
    }
}
