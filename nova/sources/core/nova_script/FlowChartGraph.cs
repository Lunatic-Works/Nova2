using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Godot;
using Nova.Exceptions;

namespace Nova;

[Flags]
public enum StartNodeType
{
    None = 0,
    Locked = 1,
    Unlocked = 2,
    Debug = 4,
    Normal = Locked | Unlocked,
    All = Locked | Unlocked | Debug
}

public readonly struct StartNode()
{
    public FlowChartNode Node { get; init; }
    public StartNodeType Type { get; init; }
    public string Name => Node.Name;
}

public readonly struct EndNode()
{
    public FlowChartNode Node { get; init; }
    public string EndName { get; init; }
}

/// <summary>
/// The data structure that stores the flow chart.
/// </summary>
/// <remarks>
/// A well-defined flow chart graph should have at least one start node, and all nodes without children are
/// marked as end nodes.
/// Everything in a flow chart graph cannot be modified after it is frozen.
/// </remarks>
public class FlowChartGraph : IEnumerable<FlowChartNode>
{
    private readonly Dictionary<string, FlowChartNode> _nodes = [];
    private readonly Dictionary<string, StartNode> _startNodes = [];
    // End nodes by node name
    private readonly Dictionary<string, EndNode> _endNodes = [];

    private bool _isFrozen;

    /// <summary>
    /// Freeze all nodes. Should be called after the construction of the flow chart graph.
    /// </summary>
    public void Freeze()
    {
        _isFrozen = true;
        foreach (var node in _nodes.Values)
        {
            node.Freeze();
        }
    }

    public void Unfreeze()
    {
        _isFrozen = false;
        foreach (var node in _nodes.Values)
        {
            node.Unfreeze();
        }
    }

    private void CheckFreeze()
    {
        Utils.RuntimeAssert(!_isFrozen, "Cannot modify a flow chart graph when it is frozen.");
    }

    public void Clear()
    {
        CheckFreeze();
        _nodes.Clear();
        _startNodes.Clear();
        _endNodes.Clear();
    }

    /// <summary>
    /// Add a node to the flow chart graph
    /// </summary>
    /// <param name="node">The node to add</param>
    /// <exception cref="ArgumentException">
    /// ArgumentException will be thrown if the name is null or empty.
    /// </exception>
    public void AddNode(FlowChartNode node)
    {
        CheckFreeze();
        if (string.IsNullOrEmpty(node.Name))
        {
            throw new ArgumentException("Nova: Node name is null or empty.");
        }
        if (_nodes.ContainsKey(node.Name))
        {
            Utils.Warn($"Nova: Overwrite node: {node.Name}");
        }
        _nodes[node.Name] = node;
    }

    /// <summary>
    /// Get a node by name
    /// </summary>
    /// <param name="name">Name of the node</param>
    /// <returns>The node if it is found, otherwise return null</returns>
    public FlowChartNode GetNode(string name)
    {
        _nodes.TryGetValue(name, out var node);
        return node;
    }

    /// <summary>
    /// Check if the graph contains the node with the given name
    /// </summary>
    /// <param name="name">Name of the node</param>
    /// <returns>True if the graph contains the given node, otherwise return false</returns>
    public bool HasNode(string name)
    {
        return _nodes.ContainsKey(name);
    }

    /// <summary>
    /// Check if the graph contains the given node
    /// </summary>
    /// <param name="node">Node to check</param>
    /// <returns>True if the graph contains the given node, otherwise return false</returns>
    public bool HasNode(FlowChartNode node)
    {
        return _nodes.ContainsKey(node.Name);
    }

    public IEnumerable<string> GetStartNodeNames(StartNodeType type)
    {
        return _startNodes.Values.Where(x => type.HasFlag(x.Type)).Select(x => x.Name);
    }

    private void CheckNode(FlowChartNode node)
    {
        CheckFreeze();
        if (!HasNode(node))
        {
            throw new ArgumentException($"Nova: Node {node.Name} is not in the graph.");
        }
    }

    /// <summary>
    /// Add a start node.
    /// </summary>
    /// <remarks>
    /// A name can be assigned to a start point, which can differ from the node name.
    /// The name should be unique among all start point names.
    /// This method will check if the given name is not in the graph, and the given node is already in the graph.
    /// </remarks>
    /// <exception cref="ArgumentException">
    /// ArgumentException will be thrown if the name is null or empty, or the node is not in the graph.
    /// </exception>
    public void AddStart(FlowChartNode node, StartNodeType type)
    {
        CheckNode(node);
        if (_startNodes.ContainsKey(node.Name))
        {
            Utils.Warn($"Nova: Overwrite start point: {node.Name}");
        }
        _startNodes[node.Name] = new StartNode { Node = node, Type = type };
    }

    /// <summary>
    /// Check if the graph contains the given start point name
    /// </summary>
    /// <param name="name">Name of the start point</param>
    /// <returns>True if the graph contains the given name, otherwise return false</returns>
    public bool HasStart(string name)
    {
        return _startNodes.ContainsKey(name);
    }

    /// <summary>
    /// Add an end node.
    /// </summary>
    /// <remarks>
    /// A name can be assigned to an end point, which can differ from the node name.
    /// The name should be unique among all end point names.
    /// This method will check if the given name is not in the graph, and the given node is already in the graph.
    /// </remarks>
    /// <param name="endName">Name of the end point</param>
    /// <param name="node">The node to add</param>
    /// <exception cref="DuplicatedDefinitionException">
    /// DuplicatedDefinitionException will be thrown if assigning two different end names to the same node.
    /// </exception>
    /// <exception cref="ArgumentException">
    /// ArgumentException will be thrown if the name is null or empty, or the node is not in the graph.
    /// </exception>
    public void AddEnd(FlowChartNode node, string endName)
    {
        CheckNode(node);
        if (string.IsNullOrEmpty(endName))
        {
            throw new ArgumentException("Nova: End name is null or empty.");
        }

        var existingEndName = GetEndName(node);
        if (existingEndName == null)
        {
            // The node has not been defined as an end
            if (HasEnd(endName))
            {
                // But the name has been used
                Utils.Warn($"Nova: Overwrite end point: {endName}");
            }

            // The name is unique, add the node as en and
            _endNodes[node.Name] = new EndNode { Node = node, EndName = endName };
        }
        else if (existingEndName != endName)
        {
            // The node has already been defined as an end.
            // But the name of the end point is different.
            throw new DuplicatedDefinitionException(
                $"Nova: Assigning two different end names to the same node: {existingEndName} and {endName}");
        }
    }

    /// <summary>
    /// Get the name of an end point
    /// </summary>
    /// <param name="node">The end node</param>
    /// <returns>
    /// The name of the end point if the node is an end node, otherwise return null
    /// </returns>
    public string GetEndName(FlowChartNode node)
    {
        _endNodes.TryGetValue(node.Name, out var end);
        return end.EndName;
    }

    /// <summary>
    /// Check if the graph contains the given end point name
    /// </summary>
    /// <param name="endName">Name of the end point</param>
    /// <returns>True if the graph contains the given name, otherwise return false</returns>
    public bool HasEnd(string endName)
    {
        return _endNodes.Values.Any(end => end.EndName == endName);
    }

    /// <summary>
    /// Perform a sanity check on the flow chart graph.
    /// </summary>
    /// <remarks>
    /// The sanity check includes:
    /// + The graph has at least one start node;
    /// + All nodes without children are marked as end nodes.
    /// This method should be called after the construction of the flow chart graph.
    /// </remarks>
    public void SanityCheck()
    {
        CheckFreeze();

        if (_startNodes.Count == 0)
        {
            throw new ArgumentException("Nova: At least one start node should exist.");
        }

        foreach (var node in _nodes.Values)
        {
            if (node.BranchCount == 0 && node.Type != FlowChartNodeType.End)
            {
                Utils.Warn($"Nova: Node {node.Name} has no child." +
                    $"It will be marked as an end with name {node.Name}.");
                node.Type = FlowChartNodeType.End;
                AddEnd(node, node.Name);
            }
        }
    }

    public IEnumerator<FlowChartNode> GetEnumerator()
    {
        return _nodes.Values.GetEnumerator();
    }

    IEnumerator IEnumerable.GetEnumerator()
    {
        return _nodes.Values.GetEnumerator();
    }
}
