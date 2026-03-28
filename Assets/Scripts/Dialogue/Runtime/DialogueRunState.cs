// Runtime state for dialogue flow.
public enum DialogueRunState
{
    // No active dialogue.
    Idle,

    // Text is being typed.
    Typing,

    // Waiting for next input.
    WaitingNext,

    // Waiting for choice selection.
    WaitingChoice
}
