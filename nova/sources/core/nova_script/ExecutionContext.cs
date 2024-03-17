namespace Nova;

public enum DialogueActionStage
{
    BeforeCheckpoint,
    Default,
    AfterDialogue
}

public enum ExecutionMode
{
    Eager,
    Lazy
}

public class ExecutionContext
{
    public readonly ExecutionMode Mode;
    public readonly DialogueActionStage Stage;
    public readonly bool IsRestoring;
}
