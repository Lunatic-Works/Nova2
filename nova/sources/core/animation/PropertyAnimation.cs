using System.Numerics;
using System.Runtime.InteropServices;
using Godot;

namespace Nova;

public class PropertyAnimation<[MustBeVariant] T> : IAnimation
{
    public PropertyState Object { get; init; }
    public StringName Property { get; init; }
    public T To { get; init; }
    public double Duration { get; init; }
    public bool Relative { get; init; }

    private Variant _fromAbsolute;
    private Variant _toAbsolute;

    public void Init()
    {
        var fromT = Object.Get(Property).As<T>();
        var toT = To;
        if (Relative)
        {
            toT += (dynamic)fromT;
        }
        _fromAbsolute = Variant.From(fromT);
        _toAbsolute = Variant.From(toT);
        GD.Print($"Init Tween {Object.Binding}.{Property} {_fromAbsolute} -> {_toAbsolute}");
        Object.Hold(Property);
        Object.Set(Property, _toAbsolute);
    }

    public bool Execute(Tween tween)
    {
        GD.Print($"Tween {Object.Binding}.{Property} {_fromAbsolute} -> {_toAbsolute}");
        var tweener = tween.TweenProperty(Object.Binding, Property.ToString(),
            _toAbsolute, Duration);
        tweener.From(_fromAbsolute);
        return true;
    }
}
