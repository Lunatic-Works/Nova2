using System;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Nova.Exceptions;
using Nova.Parser;

namespace Nova;
using ParsedBlocks = IReadOnlyList<ParsedBlock>;
using ParsedChunks = IReadOnlyList<IReadOnlyList<ParsedBlock>>;

public partial class ScriptLoader(string path) : RefCounted, ISingleton
{
    private const string DefaultPath = "scenarios";

    private readonly string _path = path;

    public ScriptLoader() : this(DefaultPath) { }

    private readonly FlowChartGraph _flowChartGraph = new();
    public FlowChartGraph FlowChartGraph
    {
        get
        {
            CheckInit();
            return _flowChartGraph;
        }
    }

    private string _currentLocale;
    private FlowChartNode _currentNode;

    private readonly struct LazyBindingEntry
    {
        public FlowChartNode From { get; init; }
        public string Destination { get; init; }
        public BranchInformation Branch { get; init; }
    }
    private readonly List<LazyBindingEntry> _lazyBindings = [];

    private static void CheckInit()
    {
        Utils.RuntimeAssert(NovaController.Instance.CheckInit<ScriptLoader>(),
            "ScriptLoader methods should be called after Init.");
    }

    private static ulong GetNodeHash(ParsedChunks nodeChunks)
    {
        return Utils.HashList(nodeChunks.SelectMany(chunk => chunk.SelectMany(block => block.ToList())));
    }

    /// <summary>
    /// Parse the given TextAsset to chunks and add them to currentNode.
    /// If deferChunks == true, only eager execution blocks are parsed and executed when the game starts
    /// If deferChunks == false, dialogues and lazy execution blocks are also parsed
    /// </summary>
    private void ParseScript(string script, bool deferChunks)
    {
        var chunks = NovaParser.ParseChunks(script);
        var nodeChunks = new List<ParsedBlocks>();
        foreach (var chunk in chunks)
        {
            var firstBlock = chunk[0];
            if (firstBlock.Type == BlockType.EagerExecution)
            {
                if (nodeChunks.Count > 0)
                {
                    if (_currentLocale == I18n.DefaultLocale)
                    {
                        _currentNode.TextHash = GetNodeHash(nodeChunks);
                    }

                    if (deferChunks)
                    {
                        _currentNode.DeferredChunks[_currentLocale] = nodeChunks;
                    }
                    else
                    {
                        AddDialogueChunks(nodeChunks);
                    }

                    nodeChunks = [];
                }

                DoEagerExecutionBlock(firstBlock.Content);
            }
            else
            {
                nodeChunks.Add(chunk);
            }
        }
    }

    private void AddDialogueChunks(ParsedChunks chunks)
    {
        if (_currentNode == null)
        {
            throw new ScriptLoadingException("Dangling node text.");
        }

        if (_currentLocale == I18n.DefaultLocale)
        {
            var entries = DialogueEntryParser.ParseDialogueEntries(chunks);
            _currentNode.SetDialogueEntries(entries);
        }
        else
        {
            var entries = DialogueEntryParser.ParseLocalizedDialogueEntries(chunks);
            _currentNode.AddLocalizedDialogueEntries(_currentLocale, entries);
        }
    }

    public static void AddDeferredDialogueChunks(FlowChartNode node)
    {
        if (node.DeferredChunks.Count == 0)
        {
            return;
        }

        node.Unfreeze();
        foreach (var (locale, chunks) in node.DeferredChunks)
        {
            if (locale == I18n.DefaultLocale)
            {
                var entries = DialogueEntryParser.ParseDialogueEntries(chunks);
                node.SetDialogueEntries(entries);
            }
            else
            {
                var entries = DialogueEntryParser.ParseLocalizedDialogueEntries(chunks);
                node.AddLocalizedDialogueEntries(locale, entries);
            }
        }
        node.DeferredChunks.Clear();
        node.Freeze();
    }

    /// <summary>
    /// Bind all lazy binding entries
    /// </summary>
    private void BindAllLazyBindingEntries()
    {
        foreach (var entry in _lazyBindings)
        {
            entry.Branch.NextNode = _flowChartGraph.GetNode(entry.Destination);
        }
        _lazyBindings.Clear();
    }

    /// <summary>
    /// Execute code in the eager execution block
    /// </summary>
    /// <param name="code"></param>
    private void DoEagerExecutionBlock(string code)
    {
        var script = GDRuntime.CompileEagerBlock(code);
        script.Call("run", this);
    }

    #region Methods called by external scripts

    /// <summary>
    /// Create a new flow chart node register it to the current constructing FlowChartGraph.
    /// If the current node is a normal node, the newly created one is intended to be its
    /// succeeding node. The link between the new node and the current one will be added immediately, which
    /// will not be registered as a lazy binding link.
    /// This method is designed to be called externally by scripts.
    /// </summary>
    /// <param name="name">Internal name of the new node</param>
    /// <param name="displayName">Displayed name of the new node</param>
    public void RegisterNewNode(string name, string displayName)
    {
        var nextNode = new FlowChartNode(name);
        if (_currentNode != null && _currentNode.Type == FlowChartNodeType.Normal)
        {
            _currentNode.AddBranch(new BranchInformation() { NextNode = nextNode });
        }

        _currentNode = nextNode;
        _flowChartGraph.AddNode(_currentNode);
        _currentNode.AddLocalizedName(_currentLocale, displayName);
    }

    public void AddLocalizedNode(string name, string displayName)
    {
        _currentNode = _flowChartGraph.GetNode(name);
        if (_currentNode == null)
        {
            throw new ScriptLoadingException(
                $"Node {name} found in {_currentLocale} but not in {I18n.DefaultLocale}. " +
                "Maybe you need to delete the default English scenarios.");
        }

        _currentNode.AddLocalizedName(_currentLocale, displayName);
    }

    /// <summary>
    /// Register a lazy binding link and null the current node.
    /// This method is designed to be called externally by scripts.
    /// </summary>
    /// <param name="destination">Destination of the jump</param>
    public void RegisterJump(string destination)
    {
        if (destination == null)
        {
            throw new ScriptLoadingException("jump_to instruction must have a destination.", _currentNode);
        }

        if (_currentNode.Type == FlowChartNodeType.Branching)
        {
            throw new ScriptLoadingException("Cannot apply jump_to() to a branching node.", _currentNode);
        }

        _lazyBindings.Add(new LazyBindingEntry { From = _currentNode, Destination = destination });
        _currentNode = null;
    }

    private static ChoiceImageInformation? ConvertImageInfo(Variant entry)
    {
        if (entry.VariantType == Variant.Type.Nil)
        {
            return null;
        }
        var dict = entry.AsGodotDictionary();
        var name = dict["name"].AsString();
        var x = (float)dict.GetValueOrDefault("x", 0d).AsDouble();
        var y = (float)dict.GetValueOrDefault("y", 0d).AsDouble();
        var scale = (float)dict.GetValueOrDefault("scale", 1d).AsDouble();
        return new() { Name = name, PositionX = x, PositionY = y, Scale = scale };
    }

    /// <summary>
    /// Add a branch to the current node.
    /// The type of the current node will be switched to Branching.
    /// This method is designed to be called externally by scripts.
    /// </summary>
    /// <param name="name">Internal name of the branch, unique in a node</param>
    /// <param name="destination">Name of the destination node</param>
    /// <param name="text">Text on the button to select this branch</param>
    /// <param name="imageInfo"></param>
    /// <param name="mode"></param>
    /// <param name="condition"></param>
    public void RegisterBranch(string name, string destination, string text, Variant imageInfo,
        BranchMode mode, string condition)
    {
        if (string.IsNullOrEmpty(destination))
        {
            throw new ScriptLoadingException("A branch must have a destination.",
                _currentNode, $"text: {text}");
        }

        if (mode == BranchMode.Normal && condition != null)
        {
            throw new ScriptLoadingException("Branch mode is Normal but condition is not null.",
                _currentNode, $"destination: {destination}");
        }

        if (mode == BranchMode.Jump && (text != null || imageInfo.VariantType == Variant.Type.Nil))
        {
            throw new ScriptLoadingException("Branch mode is Jump but text or imageInfo is not null.",
                _currentNode, $"destination: {destination}");
        }

        if ((mode == BranchMode.Show || mode == BranchMode.Enable) && condition == null)
        {
            throw new ScriptLoadingException("Branch mode is Show or Enable but condition is null.",
                _currentNode, $"destination: {destination}");
        }

        _currentNode.Type = FlowChartNodeType.Branching;
        var branch = new BranchInformation(name, text)
        {
            ImageInfo = ConvertImageInfo(imageInfo), Mode = mode,
            Condition = string.IsNullOrEmpty(condition) ? null : GDRuntime.CompileCondition(condition)
        };
        _currentNode.AddBranch(branch);
        _lazyBindings.Add(new LazyBindingEntry
        {
            From = _currentNode, Destination = destination,
            Branch = branch
        });
    }

    public void AddLocalizedBranch(string name, string destination, string text)
    {
        var branch = _lazyBindings.Find(x => x.From == _currentNode &&
            x.Destination == destination && x.Branch.Name == name).Branch ??
            throw new ScriptLoadingException("branch not found.",
                _currentNode, $"branch: {name}, destination: {destination}");
        branch.AddLocalizedText(_currentLocale, text);
    }

    /// <summary>
    /// Stop registering branches to the current node, and null the current node.
    /// This method is designed to be called externally by scripts.
    /// </summary>
    public void EndRegisterBranch()
    {
        _currentNode = null;
    }

    private void CheckNode()
    {
        if (_currentNode == null)
        {
            throw new ScriptLoadingException(
                "This function should be called after registering the current node.");
        }
    }

    public void SetCurrentAsChapter()
    {
        CheckNode();
        _currentNode.IsChapter = true;
    }

    /// <summary>
    /// Set the current node as a start node.
    /// This method is designed to be called externally by scripts.
    /// </summary>
    /// <remarks>
    /// A flow chart graph can have multiple start points.
    /// A name can be assigned to a start point, which can differ from the node name.
    /// The name should be unique among all start point names.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// ArgumentException will be thrown if called without registering the current node.
    /// </exception>
    public void SetCurrentAsStart()
    {
        CheckNode();
        _flowChartGraph.AddStart(_currentNode, StartNodeType.Locked);
    }

    public void SetCurrentAsUnlockedStart()
    {
        CheckNode();
        _flowChartGraph.AddStart(_currentNode, StartNodeType.Unlocked);
    }

    public void SetCurrentAsDebug()
    {
        CheckNode();
        _flowChartGraph.AddStart(_currentNode, StartNodeType.Debug);
    }

    /// <summary>
    /// Set the current node as an end node.
    /// This method is designed to be called externally by scripts.
    /// </summary>
    /// <remarks>
    /// A flow chart graph can have multiple end points.
    /// A name can be assigned to an end point, which can differ from the node name.
    /// The name should be unique among all end point names.
    /// </remarks>
    /// <param name="name">
    /// Name of the end point.
    /// If no name is given, the name of the current node will be used.
    /// </param>
    /// <exception cref="ArgumentException">
    /// ArgumentException will be thrown if called without registering the current node.
    /// </exception>
    public void SetCurrentAsEnd(string name)
    {
        if (_currentNode == null)
        {
            throw new ScriptLoadingException(
                $"SetCurrentAsEnd({name}) should be called after registering the current node.");
        }

        // Set the current node type as End
        _currentNode.Type = FlowChartNodeType.End;

        // Add the node as an end
        if (string.IsNullOrEmpty(name))
        {
            name = _currentNode.Name;
        }

        _flowChartGraph.AddEnd(_currentNode, name);

        // Null the current node, because SetCurrentAsEnd() indicates the end of a node
        _currentNode = null;
    }

    public bool IsDefaultLocale => _currentLocale == I18n.DefaultLocale;

    #endregion

    public void OnEnter()
    {
        _currentNode = null;
        _currentLocale = I18n.DefaultLocale;

        _flowChartGraph.Unfreeze();
        _flowChartGraph.Clear();

        foreach (var locale in I18n.SupportedLocales)
        {
            _currentLocale = locale;

            var localizedPath = Utils.ResourceRoot + _path;
            if (locale != I18n.DefaultLocale)
            {
                localizedPath = I18n.LocalizedResourcesPath + locale + "/" + _path;
            }

            if (!DirAccess.DirExistsAbsolute(localizedPath))
            {
                continue;
            }

            foreach (var fileName in DirAccess.GetFilesAt(localizedPath))
            {
                GDRuntime.BaseEagerBlock.Call("action_new_file", fileName);
                var script = Utils.GetFileAsText(localizedPath + "/" + fileName);
                try
                {
                    ParseScript(script, true);
                }
                catch (ParserException e)
                {
                    throw new ParserException($"Failed to parse {fileName}", e);
                }

                if (_currentNode != null)
                {
                    SetCurrentAsEnd(null);
                }
            }
        }

        BindAllLazyBindingEntries();

        _flowChartGraph.SanityCheck();
        _flowChartGraph.Freeze();
    }

    public void OnExit() { }
}
