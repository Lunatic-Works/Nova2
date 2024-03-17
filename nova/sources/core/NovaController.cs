using System;
using System.Collections.Generic;
using Godot;
using Nova.Exceptions;

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

    private void AddObj<T>() where T : ISingleton, new()
    {
        AddObj(new T());
    }

    private void AddObj<T>(T obj) where T : ISingleton
    {
        _objects.Add(typeof(T), obj);
        _states.Add(typeof(T), ObjectState.Uninitialized);
    }

    private void TryInit(Type type, ISingleton obj)
    {
        if (!_states.TryGetValue(type, out var state))
        {
            throw new InvalidAccessException($"Missing singleton {type}");
        }
        else if (state == ObjectState.Initializing)
        {
            throw new InvalidAccessException("Circular dependency");
        }
        else if (state == ObjectState.Initialized)
        {
            return;
        }
        _states[type] = ObjectState.Initializing;
        obj.OnEnter();
        _states[type] = ObjectState.Initialized;
    }

    public bool CheckInit<T>() where T : ISingleton
    {
        return _states.TryGetValue(typeof(T), out var state) && state == ObjectState.Initialized;
    }

    public T GetObj<T>() where T : ISingleton
    {
        var type = typeof(T);
        var obj = (T)_objects[type];
        TryInit(type, obj);
        return obj;
    }

    private void AddObjs()
    {
        AddObj<ScriptLoader>();
    }

    public override void _EnterTree()
    {
        Instance = this;
        AddObjs();
        foreach (var entry in _objects)
        {
            TryInit(entry.Key, entry.Value);
        }

    }

    public static NovaController Instance { get; private set; }

    public ScriptLoader ScriptLoader => GetObj<ScriptLoader>();
}
