using Godot;

namespace Nova;

public partial class I18nText : Control
{
    [Export]
    public string InflateTextKey;

    public override void _EnterTree()
    {
        I18n.Instance.LocaleChanged.Subscribe(UpdateText);
    }

    public override void _Ready()
    {
        UpdateText();
    }

    public override void _ExitTree()
    {
        I18n.Instance.LocaleChanged.Unsubscribe(UpdateText);
    }

    private void UpdateText()
    {
        Set("text", I18n.__(InflateTextKey));
    }
}
