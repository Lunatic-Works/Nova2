using System;
using Godot;

namespace Nova;

public partial class DialogueBoxController : PanelController
{
    public enum Mode
    {
        Overwrite,
        Append
    }

    [Export]
    private string _bindName;
    [Export]
    private Mode _mode;
    [Export]
    private Control _background;
    [Export]
    private DialogueTextController _textController;

    private float _opacity;
    private Color _backgroundColor;
    private Color _textColor;

    [Export]
    public float Opacity
    {
        get => _opacity;
        set
        {
            _opacity = value;
            UpdateBackgroundColor();
            UpdateTextColor();
        }
    }
    [Export]
    public Color BackgroundColor
    {
        get => _backgroundColor;
        set
        {
            _backgroundColor = value;
            UpdateBackgroundColor();
        }
    }
    [Export]
    public Color TextColor
    {
        get => _textColor;
        set
        {
            _textColor = value;
            UpdateTextColor();
        }
    }

    public bool IsCurrent => ViewManager.GameView.CurrentDialogueBox == this;

    public override void _EnterTree()
    {
        ObjectManager.Instance.BindObject(_bindName, this);
    }

    public override void _Ready()
    {
        UpdateBackgroundColor();
        UpdateTextColor();
    }

    private void UpdateBackgroundColor()
    {
        if (!IsNodeReady())
        {
            return;
        }
        _background.Modulate = new Color(_backgroundColor, _backgroundColor.A * _opacity);
    }

    private void UpdateTextColor()
    {
        if (!IsNodeReady())
        {
            return;
        }
        _textController.UpdateColor(_textColor);
    }

    public void DisplayDialogue(DialogueDisplayData displayData)
    {
        switch (_mode)
        {
            case Mode.Overwrite:
                OverwriteDialogue(displayData);
                break;
            case Mode.Append:
                AppendDialogue(displayData);
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    public void NewPage()
    {
        _textController.Clear();
    }

    private void AppendDialogue(DialogueDisplayData displayData)
    {
        _textController.AddEntry(displayData, _textColor);
    }

    private void OverwriteDialogue(DialogueDisplayData displayData)
    {
        NewPage();
        AppendDialogue(displayData);
    }
}
