using Godot;

namespace Nova;

public partial class RealInput : Node
{
    private static RealInput s_current;
    private Vector2 _lastMousePosition;

    public override void _Ready()
    {
        s_current = this;
    }

    public static Vector2 MousePosition
    {
        get
        {
            return s_current._lastMousePosition;
        }
    }

    public static Vector2 PointerPosition
    {
        get
        {
            return s_current._lastMousePosition;
        }
    }

    public override void _Input(InputEvent @event)
    {
        if (@event is InputEventMouseMotion eventMouseMotion)
        {
            _lastMousePosition = eventMouseMotion.Position;
        }
    }
}