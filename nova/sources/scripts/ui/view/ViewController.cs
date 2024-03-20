namespace Nova;

public partial class ViewController : PanelController
{
    public override void _EnterTree()
    {
        ViewManager.Instance.RegisterView(this);
    }
}
