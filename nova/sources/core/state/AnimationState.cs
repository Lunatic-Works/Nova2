using System.Collections.Generic;
using Godot;

namespace Nova;

public partial class AnimationState : RefCounted, IStateObject
{
    public readonly AnimationEntry Root;

    private readonly List<AnimationEntry> _animations = [];
    private readonly AnimationExecutor _executor = new();

    public bool IsRunning => _animations.Count > 0;

    public AnimationState()
    {
        Root = new(this, new DelayAnimation { Duration = 0 });
        _executor.OnFinish.Subscribe(Clear);
    }

    private void Clear()
    {
        GD.Print("Clear");
        _animations.Clear();
        Root.Children.Clear();
    }

    public void Add(AnimationEntry entry)
    {
        _animations.Add(entry);
    }

    public void Stop()
    {
        _executor.Stop();
    }

    public void Sync()
    {
        _executor.EnqueueAnimation(Root);
    }

    public void SyncImmediate()
    {
        _executor.ExecuteImmediate(Root);
    }
}
