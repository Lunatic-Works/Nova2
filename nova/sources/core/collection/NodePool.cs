using System;
using Godot;

namespace Nova;

public class NodePool<T>(Func<T> factory) : ObjectPool<T>(factory) where T : Node
{
    public NodePool(PackedScene scene) : this(() => scene.Instantiate<T>()) { }

    public T Get(Node parent, Action<T> initAction = null)
    {
        var ret = Get();
        initAction?.Invoke(ret);
        ret.RequestReady();
        parent.AddChild(ret);
        return ret;
    }

    public void Put(T obj, Node parent)
    {
        parent.RemoveChild(obj);
        Put(obj);
    }
}
