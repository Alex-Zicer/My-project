using System.Collections.Generic;

public class DialogueProviderRegistry
{
    // 已注册的数据提供器列表，按优先级顺序尝试加载。
    private readonly List<IDialogueProvider> _providers = new List<IDialogueProvider>();

    /// <summary>
    /// 初始化默认数据提供器注册顺序（SO -> JSON -> CSV）。
    /// </summary>
    public DialogueProviderRegistry()
    {
        RegisterProvider(new SoDialogueProvider());
        RegisterProvider(new JsonDialogueProvider());
        RegisterProvider(new CsvDialogueProvider());
    }

    /// <summary>
    /// 注册对话数据提供器。
    /// </summary>
    public void RegisterProvider(IDialogueProvider provider, bool prepend = false)
    {
        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (provider == null)
        {
            return;
        }

        if (prepend)
        {
            _providers.Insert(0, provider);
        }
        else
        {
            _providers.Add(provider);
        }
    }

    /// <summary>
    /// 尝试加载对话数据并输出对话图。
    /// </summary>
    public bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        if (TryLoadInternal(reference, out graph, out error))
        {
            return true;
        }

        if (reference != null && reference.fallbackSO != null)
        {
            var fallbackReference = DialogueReference.FromSo(reference.fallbackSO);
            if (TryLoadInternal(fallbackReference, out graph, out string fallbackError))
            {
                error = string.IsNullOrWhiteSpace(error)
                    ? "Primary source failed; fallback SO loaded successfully."
                    : error + "\nFallback SO loaded successfully.";
                return true;
            }

            error = string.IsNullOrWhiteSpace(error)
                ? fallbackError
                : error + "\nFallback load failed: " + fallbackError;
        }

        return false;
    }

    /// <summary>
    /// 遍历匹配的提供器执行加载。
    /// </summary>
    private bool TryLoadInternal(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        graph = null;
        error = string.Empty;

        // 守卫条件：不满足时直接返回，避免进入无效流程。
        if (reference == null)
        {
            error = "DialogueReference is null.";
            return false;
        }

        bool handledByAnyProvider = false;
        // 遍历集合并逐项处理当前业务。
        for (int i = 0; i < _providers.Count; i++)
        {
            IDialogueProvider provider = _providers[i];
            // 守卫条件：不满足时直接返回，避免进入无效流程。
            if (provider == null || !provider.CanHandle(reference))
            {
                continue;
            }

            handledByAnyProvider = true;
            if (provider.TryLoad(reference, out graph, out error))
            {
                return true;
            }
        }

        if (!handledByAnyProvider)
        {
            error = $"No provider registered for source type '{reference.sourceType}'.";
        }
        else if (string.IsNullOrWhiteSpace(error))
        {
            error = "Provider failed to load dialogue for an unknown reason.";
        }

        return false;
    }
}
