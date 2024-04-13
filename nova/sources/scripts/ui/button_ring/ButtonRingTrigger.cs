using Godot;

namespace Nova;

public partial class ButtonRingTrigger : Control
{
    private ButtonRing _buttonRing;

    public bool ButtonShowing { get; private set; }

    public override void _Ready()
    {
        _buttonRing = GetNode<ButtonRing>("Ring");
        _buttonRing.Hide();
    }

    public void ShowRing()
    {
        if (ButtonShowing) return;
        GD.Print("Show ButtonRing: " + RealInput.MousePosition);
        AdjustAnchorPosition();
        _buttonRing.Show();
        ButtonShowing = true;
    }

    public void HideRing()
    {
        if (!ButtonShowing) return;
        GD.Print("Hide ButtonRing");
        _buttonRing.Hide();
        ButtonShowing = false;
    }

    public void SetRingPosition(Vector2 position)
    {
        _buttonRing.Position = position - _buttonRing.PivotOffset;
    }

    private void AdjustAnchorPosition()
    {
        var currentPointerPos = RealInput.PointerPosition;
        SetRingPosition(currentPointerPos);
    }
}