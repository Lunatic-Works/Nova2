using System;
using Godot;

namespace Nova;

public partial class GameViewController : PanelController
{
    private GameState _gameState;
    private Label _text;

    public override void _EnterTree()
    {
        base._EnterTree();

        _gameState = GameState.Instance;

        _gameState.DialogueWillChange.Subscribe(OnDialogueWillChange);
        _gameState.DialogueChanged.Subscribe(OnDialogueChanged);
        _gameState.RouteEnded.Subscribe(OnRouteEnded);

        _text = GetNode<Label>("Text");
    }

    public override void _ExitTree()
    {
        base._ExitTree();

        _gameState.DialogueWillChange.Unsubscribe(OnDialogueWillChange);
        _gameState.DialogueChanged.Unsubscribe(OnDialogueChanged);
        _gameState.RouteEnded.Unsubscribe(OnRouteEnded);
    }

    public override void _GuiInput(InputEvent @event)
    {
        if (@event is InputEventMouseButton button &&
            button.ButtonIndex == MouseButton.Left && button.IsPressed())
        {
            GD.Print("Step");
            _gameState.Step();
        }
    }

    private void OnDialogueWillChange()
    {
        GD.Print("dialogue will change");
    }

    private void OnDialogueChanged(DialogueChangedData dialogueData)
    {
        var text = dialogueData.DisplayData.FormatNameDialogue();
        GD.Print("dialogue changed");
        _text.Text = text;
    }

    private void OnRouteEnded(ReachedEndData endData)
    {
        GD.Print($"end reached: {endData.EndName}");
        this.SwitchView<TitleController>();
    }
}
