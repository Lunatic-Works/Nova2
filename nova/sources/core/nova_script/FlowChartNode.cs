using System.Collections.Generic;
using System.Linq;
using Godot;
using Nova.Parser;
using Nova.Exceptions;

namespace Nova;
using ParsedChunks = IReadOnlyList<IReadOnlyList<ParsedBlock>>;

public enum FlowChartNodeType
{
    Normal,
    Branching,
    End
}

/// <summary>
/// A node on the flow chart
/// </summary>
/// <remarks>
/// Everything in a node cannot be modified after it is frozen
/// </remarks>
public class FlowChartNode(string name)
{
    /// <summary>
    /// Internally used name of the flow chart node.
    /// The name should be unique for each node.
    /// </summary>
    public readonly string Name = name;

    // hash value from text of the script
    public ulong TextHash;

    private bool _isFrozen;

    /// <summary>
    /// Freeze the type of this node
    /// </summary>
    public void Freeze()
    {
        _isFrozen = true;
    }

    public void Unfreeze()
    {
        _isFrozen = false;
    }

    private void CheckFreeze()
    {
        Utils.RuntimeAssert(!_isFrozen, "Cannot modify a flow chart node when it is frozen.");
    }

    private FlowChartNodeType _type = FlowChartNodeType.Normal;

    /// <summary>
    /// Type of this flow chart node. Defaults to Normal.
    /// </summary>
    /// <remarks>
    /// The type of a node is always gettable but only settable before it is frozen.
    /// A flow chart graph should freeze all its nodes after the construction.
    /// </remarks>
    public FlowChartNodeType Type
    {
        get => _type;
        set
        {
            CheckFreeze();
            _type = value;
        }
    }

    private bool _isChapter;

    public bool IsChapter
    {
        get => _isChapter;
        set
        {
            CheckFreeze();
            _isChapter = value;
        }
    }

    #region Displayed names

    /// <summary>
    /// Displayed node name in each locale.
    /// </summary>
    public readonly Dictionary<string, string> DisplayNames = [];

    public void AddLocalizedName(string locale, string displayName)
    {
        CheckFreeze();
        DisplayNames[locale] = displayName;
    }

    #endregion

    #region Dialogue entries

    /// <summary>
    /// Dialogue entries in this node.
    /// </summary>
    private IReadOnlyList<DialogueEntry> _dialogueEntries = [];

    public int DialogueEntryCount => _dialogueEntries.Count;

    public readonly Dictionary<string, ParsedChunks> DeferredChunks = [];

    public void SetDialogueEntries(IReadOnlyList<DialogueEntry> entries)
    {
        CheckFreeze();
        _dialogueEntries = entries;
    }

    public void AddLocalizedDialogueEntries(string locale, IReadOnlyList<LocalizedDialogueEntry> entries)
    {
        Utils.RuntimeAssert(entries.Count == _dialogueEntries.Count,
            $"Dialogue entry count for node {Name} in {locale} is different from that in {I18n.DefaultLocale}. " +
            "Maybe you need to delete the default English scenarios.");
        CheckFreeze();
        for (var i = 0; i < entries.Count; ++i)
        {
            _dialogueEntries[i].AddLocalized(locale, entries[i]);
        }
    }

    /// <summary>
    /// Get the dialogue entry at the given index
    /// </summary>
    /// <param name="index">The index of the element to be fetched</param>
    /// <returns>The dialogue entry at the given index</returns>
    public DialogueEntry GetDialogueEntryAt(int index)
    {
        return _dialogueEntries[index];
    }

    public IEnumerable<DialogueEntry> GetAllDialogues()
    {
        return _dialogueEntries;
    }

    #endregion

    #region Branches

    /// <summary>
    /// Branches in this node
    /// </summary>
    private readonly Dictionary<string, BranchInformation> _branches = [];

    /// <summary>
    /// The number of branches
    /// </summary>
    public int BranchCount => _branches.Count;

    /// <summary>
    /// Get the next node of a normal node. Only Normal nodes can call this property
    /// </summary>
    /// <exception cref="InvalidAccessException">
    /// An InvalidAccessException will be thrown if this node is not a Normal node
    /// </exception>
    public FlowChartNode Next
    {
        get
        {
            if (Type != FlowChartNodeType.Normal)
            {
                throw new InvalidAccessException(
                    "Nova: A flow chart node only have a next node if its type is Normal.");
            }

            return _branches[BranchInformation.DefaultName].NextNode;
        }
    }

    /// <summary>
    /// Add a branch to this node
    /// </summary>
    /// <param name="branchInformation">The information of the branch</param>
    /// <param name="nextNode">The next node at this branch</param>
    public void AddBranch(BranchInformation branch)
    {
        CheckFreeze();
        _branches.Add(branch.Name, branch);
    }

    public BranchInformation GetBranch(string name = BranchInformation.DefaultName)
    {
        return _branches.GetValueOrDefault(name, null);
    }

    /// <summary>
    /// Get all branches under this node
    /// </summary>
    /// <returns>All branch info of this node</returns>
    public IEnumerable<BranchInformation> GetAllBranches()
    {
        return _branches.Values;
    }

    /// <summary>
    /// Get next node by branch name
    /// </summary>
    /// <param name="branchName">
    /// The name of the branch.
    /// </param>
    /// <returns>
    /// The next node at the specified branch.
    /// </returns>
    public FlowChartNode GetNext(string branchName)
    {
        return _branches[branchName].NextNode;
    }

    /// <summary>
    /// Returns whether this node is a manual branch node
    /// If a branch node has all branches with jump mode, it is not manual
    /// </summary>
    public bool IsManualBranchNode()
    {
        return Type == FlowChartNodeType.Branching && _branches.Values.Any(b => b.Mode != BranchMode.Jump);
    }

    #endregion

    // FlowChartNode are considered equal if they have the same name
    public override bool Equals(object obj)
    {
        return obj is FlowChartNode other && Name == other.Name;
    }

    public override int GetHashCode()
    {
        return Name.GetHashCode();
    }

    public static bool operator ==(FlowChartNode a, FlowChartNode b) => a?.Equals(b) ?? b is null;

    public static bool operator !=(FlowChartNode a, FlowChartNode b) => !(a == b);
}
