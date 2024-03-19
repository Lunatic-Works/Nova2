using Godot;

namespace Nova;

public partial class ObjectBinder : Node
{
    [Export]
    private string _bindName;

    public override void _EnterTree()
    {
        ObjectManager.Instance.BindObject(_bindName, this);
    }
}
