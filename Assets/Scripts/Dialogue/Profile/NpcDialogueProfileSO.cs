using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "NpcDialogueProfile", menuName = "Data/Dialogue/NpcDialogueProfile")]
// NPC dialogue profile containing rules and default fallback.
public class NpcDialogueProfileSO : ScriptableObject
{
    // Profile identifier. Falls back to asset name when empty.
    public string profileId = "npc_profile";

    // Rule list.
    public List<NpcDialogueRule> rules = new List<NpcDialogueRule>();

    // Default dialogue reference when no rule matches.
    public DialogueReference defaultDialogueReference = new DialogueReference();

    // Repeat policy for default dialogue.
    public DialogueRepeatPolicy defaultRepeatPolicy = DialogueRepeatPolicy.Repeatable;

    /// <summary>
    /// Gets stable profile id.
    /// </summary>
    /// <returns>Configured profile id or asset name.</returns>
    public string GetProfileId()
    {
        return string.IsNullOrWhiteSpace(profileId) ? name : profileId;
    }
}
