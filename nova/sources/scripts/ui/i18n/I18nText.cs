using Godot;

namespace Nova;

public partial class I18nText : Label
{
    [Export]
    public string InflateTextKey;

    private Label _text;

    public override void _EnterTree()
    {
        _text = GetNode<Label>("../Label");

        UpdateText();
        I18n.LocaleChanged += UpdateText;
    }

    public override void _ExitTree()
    {
        I18n.LocaleChanged -= UpdateText;
    }

    private void UpdateText()
    {
        var str = I18n.__(InflateTextKey);

        if (_text != null) _text.Text = str;
    }
}