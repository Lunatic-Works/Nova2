using Godot;

namespace Nova;

public partial class GameViewInput : Node
{
    [Export] private ButtonRingTrigger _buttonRingTrigger;

    public override void _Input(InputEvent inputEvent)
    {
        if (inputEvent is InputEventMouseButton eventMouseButton)
        {
            if (eventMouseButton.Pressed && eventMouseButton.ButtonIndex == MouseButton.Right)
            {
                _buttonRingTrigger.ShowRing();
            }
            if (eventMouseButton.Pressed && eventMouseButton.ButtonIndex == MouseButton.Left)
            {
                _buttonRingTrigger.HideRing();
            }
        }
    }
}