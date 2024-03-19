using System.Collections.Generic;
using Godot;

namespace Nova;

public partial class ObjectManager : RefCounted, ISingleton
{
    public Godot.Collections.Dictionary<string, GodotObject> Objects { get; private set; }
    public Godot.Collections.Dictionary<string, Variant> Constants = new(new Dictionary<string, Variant>()
    {
        ["resource_root"] = Assets.ResourceRoot,
        ["nova_resource_root"] = Assets.NovaResourceRoot,
    });

    public void OnEnter()
    {
        Constants.MakeReadOnly();
        Objects = [];
    }

    public void OnReady()
    {
        // objects must bind before _Ready
        Objects.MakeReadOnly();
    }

    public void OnExit() { }

    public void BindObject(string name, GodotObject obj)
    {
        if (Objects.ContainsKey(name))
        {
            Utils.Warn($"Binding object already exists: {name}");
        }
        Objects.Add(name, obj);
    }

    public static ObjectManager Instance => NovaController.Instance.ObjectManager;
}
