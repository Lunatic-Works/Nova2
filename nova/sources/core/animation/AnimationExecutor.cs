using System.Collections.Generic;
using Godot;

namespace Nova;

public class AnimationExecutor
{
    private readonly HashSet<AnimationEntry> _runningPool = [];
    private readonly Queue<AnimationEntry> _queue = [];

    public readonly Event OnFinish = new();

    private void OnFinishEntry(AnimationEntry entry, bool result)
    {
        _runningPool.Remove(entry);
        entry.Tween = null;
        if (result)
        {
            foreach (var child in entry.Children)
            {
                EnqueueAnimation(child);
            }
        }
        if (_runningPool.Count <= 0)
        {
            OnFinish.Invoke();
        }
    }

    public void EnqueueAnimation(AnimationEntry entry)
    {
        if (_runningPool.Contains(entry))
        {
            Utils.Warn($"Animation already playing");
            return;
        }
        _runningPool.Add(entry);
        var tween = Utils.CurrentSceneTree.CreateTween();
        tween.TweenInterval(0);
        entry.Tween = tween;
        var result = entry.Animation.Execute(tween);
        tween.Finished += () => OnFinishEntry(entry, result);
    }

    private void ExecuteImmediate()
    {
        while (_queue.Count > 0)
        {
            var entry = _queue.Dequeue();
            var result = entry.Animation.ExecuteImmediate();
            if (result)
            {
                foreach (var child in entry.Children)
                {
                    _queue.Enqueue(child);
                }
            }
        }
        OnFinish.Invoke();
    }

    public void ExecuteImmediate(AnimationEntry root)
    {
        _queue.Clear();
        _queue.Enqueue(root);
        ExecuteImmediate();
    }

    public void Stop()
    {
        _queue.Clear();
        foreach (var entry in _runningPool)
        {
            entry.Tween.Kill();
            entry.Tween = null;
            _queue.Enqueue(entry);
        }
        _runningPool.Clear();
        // immediately execute the remaining animation
        ExecuteImmediate();
    }
}
