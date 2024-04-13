using Godot;

namespace Nova;

public partial class ButtonRingTrigger : Control
{
    [Export]
    private ButtonRing _buttonRing;

    public bool ButtonShowing => _buttonRing.Visible;

    public void ShowRing(Vector2 position)
    {
        if (ButtonShowing) return;
        AdjustAnchorPosition(position);
        _buttonRing.Show();
    }

    public void HideRing()
    {
        if (!ButtonShowing) return;
        GD.Print("Hide ButtonRing");
        _buttonRing.Hide();
    }

    public void SetRingPosition(Vector2 position)
    {
        _buttonRing.Position = position - _buttonRing.PivotOffset;
    }

    private void AdjustAnchorPosition(Vector2 position)
    {
        SetRingPosition(position);
    }
}
