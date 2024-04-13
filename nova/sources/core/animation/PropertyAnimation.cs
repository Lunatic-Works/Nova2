using System.Numerics;
using Godot;

namespace Nova;

public class PropertyAnimation<[MustBeVariant] T> : IAnimation
{
    public GodotObject Object { get; init; }
    public NodePath Property { get; init; }
    public T From { get; init; }
    public T To { get; init; }
    public double Duration { get; init; }
    public bool FromCurrent { get; init; }
    public bool Relative { get; init; }

    public bool Execute(Tween tween)
    {
        var tweener = tween.TweenProperty(Object, Property, Variant.From(To), Duration);

        if (Relative)
        {
            tweener.AsRelative();
        }
        else if (FromCurrent)
        {
            tweener.FromCurrent();
        }
        else
        {
            tweener.From(Variant.From(From));
        }
        return true;
    }

    public bool ExecuteImmediate()
    {
        var to = To;
        var prop = Property.ToString();
        if (Relative)
        {
            to += (dynamic)Object.Get(Property.GetConcatenatedSubNames()).As<T>();
        }
        Object.Set(prop, Variant.From(to));
        return true;
    }
}
