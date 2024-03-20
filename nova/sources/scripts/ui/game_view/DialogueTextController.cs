using System.Collections.Generic;
using Godot;

namespace Nova;

public partial class DialogueTextController : Control
{
    [Export]
    private PackedScene _entryFactory;
    private ObjectPool<DialogueEntryController> _pool;

    private readonly List<DialogueEntryController> _entries = [];
    public IReadOnlyList<DialogueEntryController> Entries => _entries;
    public int EntryCount => _entries.Count;

    public override void _EnterTree()
    {
        _pool = new(() => _entryFactory.Instantiate<DialogueEntryController>());
    }

    public void Clear()
    {
        foreach (var entry in _entries)
        {
            RemoveChild(entry);
            _pool.Put(entry);
        }
        _entries.Clear();
    }

    public DialogueEntryController AddEntry(DialogueDisplayData displayData, Color textColor)
    {
        var entry = _pool.Get();
        entry.Init(displayData, textColor);
        AddChild(entry);
        _entries.Add(entry);
        return entry;
    }

    public void UpdateColor(Color color)
    {
        foreach (var entry in _entries)
        {
            entry.TextColor = color;
        }
    }
}
