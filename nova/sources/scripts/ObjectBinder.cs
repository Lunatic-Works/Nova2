using Godot;

namespace Nova;

public partial class ObjectBinder : Node
{
    [Export]
    private string _bindName;

    public override void _EnterTree()
    {
        if (string.IsNullOrEmpty(_bindName))
        {
            Utils.Warn($"Empty binding name on {this}");
            return;
        }
        var state = new PropertyState(this);
        StateManager.Instance.BindPropertyState(_bindName, state);
    }
}
