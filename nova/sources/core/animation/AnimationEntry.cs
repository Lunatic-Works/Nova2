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

    private AnimationEntry Property<[MustBeVariant] T>(PropertyState obj, StringName property, T to,
        double duration, bool relative)
    {
        var animation = new PropertyAnimation<T>()
        {
            Object = obj,
            Property = property,
            To = to,
            Duration = duration,
            Relative = relative,
        };
        return Entry(animation);
    }

    public AnimationEntry PropertyVector3(PropertyState obj, StringName property, Vector3 to,
        double duration, bool relative)
    {
        return Property(obj, property, to, duration, relative);
    }

    public AnimationEntry PropertyColor(PropertyState obj, StringName property, Color to,
        double duration, bool relative)
    {
        return Property(obj, property, to, duration, relative);
    }

    public AnimationEntry PropertyDouble(PropertyState obj, StringName property, double to,
        double duration, bool relative)
    {
        return Property(obj, property, to, duration, relative);
    }

    public AnimationEntry Delay(double duration)
    {
        return Entry(new DelayAnimation { Duration = duration });
    }
}
