using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Nova;

public partial class PropertyState : RefCounted, IStateObject
{
    public readonly GodotObject Binding;

    private readonly Dictionary<StringName, Variant> _properties = [];
    private readonly HashSet<StringName> _dirtyProperties = [];
    private readonly Dictionary<StringName, Variant> _holdingProperties = [];
    private readonly Godot.Collections.Array<Godot.Collections.Dictionary> _propertyList;
    private readonly HashSet<StringName> _propertyNames;

    public PropertyState(GodotObject binding)
    {
        Binding = binding;
        _propertyList = binding.GetPropertyList();
        _propertyNames = _propertyList.Select(
            entry => entry["name"].AsStringName()).ToHashSet();
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
        _dirtyProperties.Add(key);
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
        var refreshed = new List<StringName>();
        foreach (var key in _dirtyProperties)
        {
            if (!_holdingProperties.TryGetValue(key, out var value))
            {
                value = _properties[key];
                refreshed.Add(key);
            }
            GD.Print($"Sync {Binding}.{key} = {value}");
            Binding.Set(key, value);
        }
        _dirtyProperties.ExceptWith(refreshed);
    }

    public void SyncImmediate()
    {
        _holdingProperties.Clear();
        Sync();
    }

    public void SyncBackend() { }

    public void Hold(StringName key)
    {
        var value = Get(key);
        GD.Print($"Hold {Binding}.{key} = {value}");
        _holdingProperties.Add(key, value);
    }

    public override Variant _Get(StringName key)
    {
        if (_properties.TryGetValue(key, out var value))
        {
            return value;
        }
        return Binding.Get(key);
    }

    public override bool _Set(StringName key, Variant value)
    {
        GD.Print($"Set {Binding}.{key} = {value}");
        _properties[key] = value;
        _dirtyProperties.Add(key);
        return true;
    }

    public override Godot.Collections.Array<Godot.Collections.Dictionary> _GetPropertyList()
    {
        return _propertyList;
    }
}
