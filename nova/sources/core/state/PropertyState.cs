using System.Collections.Generic;
using Godot;

namespace Nova;

public partial class PropertyState(GodotObject binding) : RefCounted, IStateObject
{
    public readonly GodotObject Binding = binding;

    private bool _dirty = true;
    private readonly Dictionary<StringName, Variant> _properties = [];

    public void Sync()
    {
        foreach (var entry in _properties)
        {
            GD.Print($"Sync {Binding}.{entry.Key} = {entry.Value}");
            Binding.Set(entry.Key, entry.Value);
        }
        _properties.Clear();
    }

    public void SyncImmediate()
    {
        Sync();
    }

    // do nothing during sync backend
    public void SyncBackend() { }

    public override Variant _Get(StringName property)
    {
        if (_properties.TryGetValue(property, out var value))
        {
            return value;
        }
        return Binding.Get(property);
    }

    public override bool _Set(StringName property, Variant value)
    {
        _properties[property] = value;
        return true;
    }

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        return Binding.GetPropertyList();
    }
}
