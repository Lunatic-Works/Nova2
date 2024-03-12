using System;
using System.Collections.Generic;
using Godot;

namespace Nova;

public partial class NovaController : Node
{
    private enum ObjectState
    {
        Uninitialized,
        Initializing,
        Initialized
    }

    private readonly Dictionary<Type, ISingleton> _objects = [];
    private readonly Dictionary<Type, ObjectState> _states = [];

    private void Add<T>() where T : ISingleton, new()
    {
        _objects.Add(typeof(T), new T());
        _states.Add(typeof(T), ObjectState.Uninitialized);
    }

    private void TryInit(Type type, ISingleton obj)
    {
        if (!_states.TryGetValue(type, out var state))
        {
            throw new InvalidOperationException($"Missing singleton {type}");
        }
        else if (state == ObjectState.Initializing)
        {
            throw new InvalidOperationException("Circular dependency");
        }
        else if (state == ObjectState.Initialized)
        {
            return;
        }
        _states[type] = ObjectState.Initializing;
        obj.OnEnter();
        _states[type] = ObjectState.Initialized;
    }

    public T Get<T>() where T : ISingleton
    {
        var type = typeof(T);
        var obj = (T)_objects[type];
        TryInit(type, obj);
        return obj;
    }

    private void AddObjs()
    {
        // place singletons here
    }

    public override void _EnterTree()
    {
        AddObjs();
        foreach (var entry in _objects)
        {
            TryInit(entry.Key, entry.Value);
        }
    }
}