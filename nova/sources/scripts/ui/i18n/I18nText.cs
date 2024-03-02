using Godot;
namespace Nova;

public partial class I18nText : Label
{
    [Export]
    public string inflateTextKey;

    private Label text;

    public override void _EnterTree()
    {
        text = GetNode<Label>("../Label");

        UpdateText();
        I18n.LocaleChanged += UpdateText;
    }

    public override void _ExitTree()
    {
        I18n.LocaleChanged -= UpdateText;
    }

    private void UpdateText()
    {
        string str = I18n.__(inflateTextKey);

        if (text != null) text.Text = str;
    }
}