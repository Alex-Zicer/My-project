using System.Collections.Generic;

// Provider 注册表：
// 统一管理“由哪个 Provider 处理哪种引用”，并负责主来源失败后的 fallback 兜底策略。
public class DialogueProviderRegistry
{
    // 按注册顺序匹配 Provider；可通过 prepend 把高优先级 Provider 插到前面。
    private readonly List<IDialogueProvider> _providers = new List<IDialogueProvider>();

    public DialogueProviderRegistry()
    {
        // 默认注册顺序：SO -> JSON -> CSV。
        // 如需覆盖行为，可在外部二次注册自定义 Provider。
        RegisterProvider(new SoDialogueProvider());
        RegisterProvider(new JsonDialogueProvider());
        RegisterProvider(new CsvDialogueProvider());
    }

    // 注册 Provider。
    // prepend=true 时插队到前面，用于覆盖默认解析逻辑。
    public void RegisterProvider(IDialogueProvider provider, bool prepend = false)
    {
        if (provider == null) return;
        if (prepend) _providers.Insert(0, provider);
        else _providers.Add(provider);
    }

    // 加载入口：
    // 1) 先尝试主引用；
    // 2) 主引用失败且配置了 fallbackSO 时，自动尝试回退。
    public bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        if (TryLoadInternal(reference, out graph, out error))
        {
            return true;
        }

        // 主来源失败后尝试回退 SO，避免对话完全不可用。
        if (reference != null && reference.fallbackSO != null)
        {
            var fallbackReference = DialogueReference.FromSo(reference.fallbackSO);
            if (TryLoadInternal(fallbackReference, out graph, out string fallbackError))
            {
                error = string.IsNullOrWhiteSpace(error)
                    ? "主数据源加载失败，已成功使用回退 SO。"
                    : error + "\n回退 SO 加载成功。";
                return true;
            }

            error = string.IsNullOrWhiteSpace(error)
                ? fallbackError
                : error + "\n回退加载失败: " + fallbackError;
        }

        return false;
    }

    // 内部加载流程：
    // 顺序遍历可处理该引用的 Provider，谁先成功谁返回。
    private bool TryLoadInternal(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        graph = null;
        error = string.Empty;

        if (reference == null)
        {
            error = "DialogueReference 为空。";
            return false;
        }

        bool handledByAnyProvider = false;
        foreach (IDialogueProvider provider in _providers)
        {
            if (provider == null || !provider.CanHandle(reference)) continue;

            handledByAnyProvider = true;
            // Provider 负责返回“可读错误”，因此这里直接透传。
            if (provider.TryLoad(reference, out graph, out error))
            {
                return true;
            }
        }

        if (!handledByAnyProvider)
        {
            error = $"没有可处理来源类型 '{reference.sourceType}' 的 Provider。";
        }
        else if (string.IsNullOrWhiteSpace(error))
        {
            error = "Provider 加载对话失败，原因未知。";
        }

        return false;
    }
}
