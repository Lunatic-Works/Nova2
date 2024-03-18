namespace Nova;

public readonly struct ReachedEndData()
{
    public string EndName { get; init; }
}

public readonly struct ReachedDialogueData()
{
    public string NodeName { get; init; }
    public int DialogueIndex { get; init; }
    // public readonly VoiceEntries voices;
    public bool NeedInterpolate { get; init; }
    public ulong TextHash { get; init; }
}
