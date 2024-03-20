using System.Collections.Generic;
using Godot;

namespace Nova;

public partial class DialogueTextController : Control
{
    [Export]
    private PackedScene _entryFactory;
    private NodePool<DialogueEntryController> _pool;

    private readonly List<DialogueEntryController> _entries = [];
    public IReadOnlyList<DialogueEntryController> Entries => _entries;
    public int EntryCount => _entries.Count;

    public override void _EnterTree()
    {
        _pool = new(_entryFactory);
    }

    public void Clear()
    {
        foreach (var entry in _entries)
        {
            _pool.Put(entry, this);
        }
        _entries.Clear();
    }

    public DialogueEntryController AddEntry(DialogueDisplayData displayData, Color textColor)
    {
        var entry = _pool.Get(this, e => e.Init(displayData, textColor));
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
