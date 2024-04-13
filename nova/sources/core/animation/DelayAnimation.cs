using Godot;

namespace Nova;

public class DelayAnimation : IAnimation
{
    public double Duration { get; init; }

    public bool Execute(Tween tween)
    {
        tween.TweenInterval(Duration);
        return true;
    }

    public bool ExecuteImmediate()
    {
        return true;
    }
}
