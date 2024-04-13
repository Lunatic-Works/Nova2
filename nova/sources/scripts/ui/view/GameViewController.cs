using System;
using Godot;

namespace Nova;

public partial class GameViewController : ViewController
{
    [Export]
    private PanelController _gameUI;

    private GameState _gameState;
    private AnimationState _animation;

    public DialogueBoxController CurrentDialogueBox { get; set; }

    public bool UIActive => _gameUI.Active;

    public override void _EnterTree()
    {
        base._EnterTree();

        _gameState = GameState.Instance;

        _gameState.DialogueWillChange.Subscribe(OnDialogueWillChange);
        _gameState.DialogueChanged.Subscribe(OnDialogueChanged);
        _gameState.RouteEnded.Subscribe(OnRouteEnded);

        _animation = StateManager.Instance.Animation;
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
            if (_animation.IsRunning)
            {
                _animation.Stop();
            }
            else
            {
                _gameState.Step();
            }
        }
    }

    private void OnDialogueWillChange()
    {
        GD.Print("dialogue will change");
    }

    private void OnDialogueChanged(DialogueChangedData dialogueData)
    {
        GD.Print("dialogue changed");
        CurrentDialogueBox?.DisplayDialogue(dialogueData.DisplayData);
    }

    private void OnRouteEnded(ReachedEndData endData)
    {
        GD.Print($"end reached: {endData.EndName}");
        this.SwitchView<TitleController>();
    }

    public void ShowUI(Action onFinish)
    {
        _gameUI.ShowPanel(onFinish);
    }

    public void HideUI(Action onFinish)
    {
        _gameUI.HidePanel(onFinish);
    }

    // for gdscript
    public void ShowUI()
    {
        ShowUI(null);
    }

    public void HideUI()
    {
        _gameUI.HidePanel(null);
    }

    public void SwitchDialogueBox(DialogueBoxController box, bool cleanText = true)
    {
        if (CurrentDialogueBox == box)
        {
            box?.ShowPanelImmediate();
            // Do not clean text
            return;
        }

        CurrentDialogueBox?.HidePanelImmediate();
        if (box != null)
        {
            box.ShowPanelImmediate();
            if (cleanText)
            {
                box.NewPage();
            }
        }

        CurrentDialogueBox = box;
    }
}
