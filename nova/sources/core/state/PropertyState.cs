using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Nova;

public partial class PropertyState : RefCounted, IStateObject
{
    public readonly GodotObject Binding;

    private readonly Dictionary<StringName, Variant> _dirtyProperties = [];
    private readonly Dictionary<StringName, Variant> _properties = [];
    private readonly Godot.Collections.Array<Godot.Collections.Dictionary> _propertyList;
    private readonly HashSet<string> _propertyNames;

    public PropertyState(GodotObject binding)
    {
        Binding = binding;
        _propertyList = binding.GetPropertyList();
        _propertyNames = _propertyList.Select(entry => entry["name"].AsString()).ToHashSet();
    }

    private void AddProperty(StringName key, Variant value)
    {
        if (!_propertyNames.Contains(key))
        {
            var entry = new Godot.Collections.Dictionary()
            {
                ["name"] = key,
                ["type"] = (int)value.VariantType,
                ["usage"] = (int)PropertyUsageFlags.NoEditor,
                ["hint"] = (int)PropertyHint.None,
                ["hint_string"] = "",
            };
            _propertyList.Add(entry);
            _propertyNames.Add(key);
        }
        _properties.Add(key, value);
        _dirtyProperties.Add(key, value);
    }

    public Variant this[StringName key] { init => AddProperty(key, value); }

    public List<StringName> InitProperties
    {
        init
        {
            foreach (var key in value)
            {
                AddProperty(key, Binding.Get(key));
            }
        }
    }

    public void Sync()
    {
        foreach (var entry in _dirtyProperties)
        {
            GD.Print($"Sync {Binding}.{entry.Key} = {entry.Value}");
            Binding.Set(entry.Key, entry.Value);
        }
        _dirtyProperties.Clear();
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
        _dirtyProperties[property] = value;
        _properties[property] = value;
        return true;
    }

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        return _propertyList;
    }
}
