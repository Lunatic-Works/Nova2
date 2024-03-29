using Godot;

namespace Nova;

public partial class DialogueEntryController : Control
{
    [Export]
    private Label _nameText;
    [Export]
    private RichTextLabel _contentText;

    public DialogueDisplayData DisplayData { get; private set; }
    private Color _textColor;
    public Color TextColor
    {
        get => _textColor;
        set
        {
            _textColor = value;
            UpdateColor();
        }
    }

    public override void _EnterTree()
    {
        I18n.Instance.LocaleChanged.Subscribe(UpdateText);
    }

    public override void _Ready()
    {
        UpdateText();
        UpdateColor();
    }

    public override void _ExitTree()
    {
        I18n.Instance.LocaleChanged.Unsubscribe(UpdateText);
        // clear references
        DisplayData = default;
        TextColor = default;
    }

    private void UpdateText()
    {
        if (!IsNodeReady())
        {
            return;
        }
        var name = I18n.__(DisplayData.DisplayNames);
        _nameText.Visible = !string.IsNullOrEmpty(name);
        _nameText.Text = name;
        _contentText.Text = I18n.__(DisplayData.Dialogues);
    }

    private void UpdateColor()
    {
        if (!IsNodeReady())
        {
            return;
        }
        _nameText.AddThemeColorOverride("font_color", TextColor);
        _contentText.AddThemeColorOverride("default_color", TextColor);
    }

    public void Init(DialogueDisplayData displayData, Color textColor)
    {
        DisplayData = displayData;
        TextColor = textColor;
    }
}
