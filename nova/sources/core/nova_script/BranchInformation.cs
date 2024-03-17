using System.Collections.Generic;
using Godot;

namespace Nova;

public enum BranchMode
{
    Normal,
    Jump,
    Show,
    Enable
}

public readonly struct ChoiceImageInformation
{
    public string Name { get; init; }
    public float PositionX { get; init; }
    public float PositionY { get; init; }
    public float Scale { get; init; }
}

/// <summary>
/// The information of branch
/// </summary>
/// <remarks>
/// BranchInformation is immutable
/// </remarks>
public class BranchInformation
{
    /// <summary>
    /// The internal name of the branch, auto generated from ScriptLoader.RegisterBranch()
    /// The name should be unique in a flow chart node
    /// </summary>
    public string Name { get; init; }
    public Dictionary<string, string> Texts { get; init; }
    public BranchMode Mode { get; init; }
    public ChoiceImageInformation? ImageInfo { get; init; }
    public RefCounted Condition { get; init; }
    public FlowChartNode NextNode { get; set; }

    /// <summary>
    /// The default branch, used in normal flow chart nodes
    /// </summary>
    /// <remarks>
    /// Since the default branch owns the default name, all other branches should not have the name '__default'
    /// </remarks>
    public const string Default = "__default";

    public BranchInformation(string name = Default, string text = null)
    {
        Name = name;
        Texts = text != null ? new() { [I18n.DefaultLocale] = text } : [];
        Mode = BranchMode.Normal;
    }

    public void AddLocalizedText(string locale, string text)
    {
        Texts[locale] = text;
    }
}
