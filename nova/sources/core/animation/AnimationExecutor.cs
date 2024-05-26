using System.Collections.Generic;

namespace Nova;

public class AnimationExecutor
{
    private readonly HashSet<AnimationEntry> _runningPool = [];

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
        entry.Tween = tween;
        var result = entry.Animation.Execute(tween);
        tween.Finished += () => OnFinishEntry(entry, result);
    }

    public void Stop()
    {
        foreach (var entry in _runningPool)
        {
            entry.Tween.Kill();
            entry.Tween = null;
        }
        if (_runningPool.Count > 0)
        {
            OnFinish.Invoke();
        }
        _runningPool.Clear();
    }
}
