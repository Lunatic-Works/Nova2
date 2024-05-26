using System.Collections.Generic;
using Godot;

namespace Nova;

public partial class AnimationState : RefCounted, IStateObject
{
    public readonly AnimationEntry Root;
    public readonly Event OnFinish = new();

    private readonly List<AnimationEntry> _animations = [];
    private readonly AnimationExecutor _executor = new();

    public bool IsRunning => _animations.Count > 0;

    public AnimationState()
    {
        Root = AnimationEntry.Root(this);
        _executor.OnFinish.Subscribe(Finish);
    }

    private void Finish()
    {
        _animations.Clear();
        Root.Children.Clear();
        OnFinish.Invoke();
    }

    public void Add(AnimationEntry entry)
    {
        _animations.Add(entry);
    }

    public void Stop()
    {
        _executor.Stop();
    }

    public void Play()
    {
        _executor.EnqueueAnimation(Root);
    }

    public void Sync()
    {
        Play();
    }

    public void SyncImmediate()
    {
        Stop();
    }

    public void SyncBackend()
    {
        Stop();
    }
}
