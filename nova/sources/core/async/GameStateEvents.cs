using System.Collections.Generic;

namespace Nova;

public readonly struct NodeChangedData()
{
    public string NewNode { get; init; }
}

public readonly struct DialogueChangedData()
{
    // TODO
    public ReachedDialogueData DialogueData { get; init; }
    public DialogueDisplayData DisplayData { get; init; }
    public bool IsReached { get; init; }
    public bool IsReachedAnyHistory { get; init; }
}

public readonly struct ChoiceData()
{
    public Dictionary<string, string> Texts { get; init; }
    public ChoiceImageInformation? ImageInfo { get; init; }
    public bool Interactable { get; init; }

    public ChoiceData(string text, ChoiceImageInformation? imageInfo, bool interactable) : this()
    {
        Texts = new() { [I18n.DefaultLocale] = text };
        ImageInfo = imageInfo;
        Interactable = interactable;
    }
}

public readonly struct ChoiceOccursData()
{
    public IReadOnlyList<ChoiceData> Choices { get; init; }
}
