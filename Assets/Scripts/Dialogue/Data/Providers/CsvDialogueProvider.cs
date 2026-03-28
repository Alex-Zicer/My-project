// CSV provider placeholder.
public class CsvDialogueProvider : IDialogueProvider
{
    /// <summary>
    /// Checks whether this provider can handle the input reference.
    /// </summary>
    /// <param name="reference">Dialogue reference.</param>
    /// <returns>True when source type is CSV.</returns>
    public bool CanHandle(DialogueReference reference)
    {
        return reference != null && reference.sourceType == DialogueSourceType.Csv;
    }

    /// <summary>
    /// Attempts to load a dialogue graph from CSV data.
    /// </summary>
    /// <param name="reference">Dialogue reference.</param>
    /// <param name="graph">Loaded graph output.</param>
    /// <param name="error">Error message output.</param>
    /// <returns>Always false in current placeholder implementation.</returns>
    public bool TryLoad(DialogueReference reference, out DialogueGraph graph, out string error)
    {
        graph = null;
        error = "CSV provider is not implemented yet.";
        return false;
    }
}
