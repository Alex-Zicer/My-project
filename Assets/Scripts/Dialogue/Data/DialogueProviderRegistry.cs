using System.Collections.Generic;

// Provider registry for dialogue data loading.
public class DialogueProviderRegistry
{
    // Registered providers in resolution order.
    private readonly List<IDialogueProvider> _providers = new List<IDialogueProvider>();

    /// <summary>
    /// Initializes registry with built-in providers.
    /// </summary>
    public DialogueProviderRegistry()
    {
        // Default priority: SO, JSON, CSV.
        RegisterProvider(new SoDialogueProvider());
        RegisterProvider(new JsonDialogueProvider());
        RegisterProvider(new CsvDialogueProvider());
    }

    /// <summary>
    /// Registers a provider.
    /// </summary>
    /// <param name="provider">Provider instance.</param>
    /// <param name="prepend">True to insert at the front.</param>
    public void RegisterProvider(IDialogueProvider provider, bool prepend = false)
    {
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
    /// Tries to load dialogue from primary source, then optional fallback SO.
    /// </summary>
    /// <param name="reference">Dialogue reference.</param>
    /// <param name="graph">Loaded graph output.</param>
    /// <param name="error">Error message output.</param>
    /// <returns>True when any load path succeeds.</returns>
    public bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        if (TryLoadInternal(reference, out graph, out error))
        {
            return true;
        }

        // If primary source fails, try fallback SO if configured.
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
    /// Tries to load dialogue by iterating matching providers.
    /// </summary>
    /// <param name="reference">Dialogue reference.</param>
    /// <param name="graph">Loaded graph output.</param>
    /// <param name="error">Error message output.</param>
    /// <returns>True when one provider succeeds.</returns>
    private bool TryLoadInternal(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        graph = null;
        error = string.Empty;

        if (reference == null)
        {
            error = "DialogueReference is null.";
            return false;
        }

        bool handledByAnyProvider = false;
        for (int i = 0; i < _providers.Count; i++)
        {
            IDialogueProvider provider = _providers[i];
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
