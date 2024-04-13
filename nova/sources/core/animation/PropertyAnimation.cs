using System.Numerics;
using Godot;

namespace Nova;

public class PropertyAnimation<[MustBeVariant] T> : IAnimation
{
    public PropertyState Object { get; init; }
    public StringName Property { get; init; }
    public T To { get; init; }
    public double Duration { get; init; }
    public bool Relative { get; init; }

    private void GetFromTo(out Variant from, out Variant to)
    {
        var fromT = Object.Get(Property).As<T>();
        var toT = To;
        if (Relative)
        {
            toT += (dynamic)fromT;
        }
        from = Variant.From(fromT);
        to = Variant.From(toT);
    }

    public bool Execute(Tween tween)
    {
        GetFromTo(out var from, out var to);
        GD.Print($"Tween {Object.Binding}.{Property} {from} -> {to}");
        var tweener = tween.TweenProperty(Object.Binding, Property.ToString(), to, Duration);
        tweener.From(from);
        return true;
    }

    public bool ExecuteImmediate()
    {
        GetFromTo(out _, out var to);
        GD.Print($"Set {Object.Binding}.{Property} = {to}");
        Object.Set(Property, to);
        return true;
    }
}
