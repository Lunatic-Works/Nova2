using System.Collections.Generic;
using Godot;

namespace Nova;

public partial class AnimationEntry(AnimationState animationState, IAnimation animation) : RefCounted
{
    private readonly AnimationState _animationState = animationState;
    public readonly IAnimation Animation = animation;
    public readonly List<AnimationEntry> Children = [];
    public Tween Tween = null;

    private AnimationEntry Entry(IAnimation animation)
    {
        var entry = new AnimationEntry(_animationState, animation);
        Children.Add(entry);
        _animationState.Add(entry);
        return entry;
    }

    private AnimationEntry Property<[MustBeVariant] T>(GodotObject obj, NodePath property, T to, double duration)
    {
        var animation = new PropertyAnimation<T>()
        {
            Object = obj,
            Property = property,
            To = to,
            Duration = duration,
            FromCurrent = true,
        };
        return Entry(animation);
    }

    public AnimationEntry PropertyVector3(GodotObject obj, NodePath property, Vector3 to, double duration)
    {
        return Property(obj, property, to, duration);
    }

    public AnimationEntry PropertyColor(GodotObject obj, NodePath property, Color to, double duration)
    {
        return Property(obj, property, to, duration);
    }

    public AnimationEntry PropertyDouble(GodotObject obj, NodePath property, double to, double duration)
    {
        return Property(obj, property, to, duration);
    }

    public AnimationEntry Delay(double duration)
    {
        return Entry(new DelayAnimation { Duration = duration });
    }
}
