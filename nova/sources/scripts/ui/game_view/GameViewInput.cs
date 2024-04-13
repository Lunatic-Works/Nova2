using Godot;

namespace Nova;

public class GameViewInput
{
    private readonly ButtonRingTrigger _buttonRingTrigger;
    private readonly GameViewController _gameView;

    private bool _needShowUI => !_gameView.UIActive;

    public ButtonRingTrigger ButtonRingTrigger { init => _buttonRingTrigger = value; }

    public GameViewInput(GameViewController gameView)
    {
        _gameView = gameView;

    }

    private void ClickForward()
    {
        GD.Print("Click Forward");
        _gameView.Step();
    }

    private void OnMouseDown(InputEventMouseButton @event)
    {

    }

    private void OnMouseUp(InputEventMouseButton @event)
    {
        if (_needShowUI)
        {
            _gameView.ShowUI();
            return;
        }
        if (_buttonRingTrigger.ButtonShowing)
        {
            _buttonRingTrigger.HideRing();
            return;
        }

        if (@event.ButtonIndex == MouseButton.Left)
        {
            ClickForward();
        }

        if (@event.ButtonIndex == MouseButton.Right)
        {
            _buttonRingTrigger.ShowRing(@event.Position);
        }
    }

    public void HandleMouseButton(InputEventMouseButton @event)
    {
        if (@event.Pressed)
        {
            OnMouseDown(@event);
        }
        else
        {
            OnMouseUp(@event);
        }
    }
}
