using System;
using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Nova;

public partial class ChapterSelectController : PanelController
{
    [Export]
    private bool _unlockAllNodes;
    [Export]
    private bool _unlockDebugNodes;
    [Export]
    private Node _chapterList;

    private GameState _gameState;

    private List<string> _nodes;
    private HashSet<string> _activeNodes;
    private HashSet<string> _unlockedNodes;
    private List<Button> _buttons;

    public override void _EnterTree()
    {
        base._EnterTree();

        _gameState = GameState.Instance;

        // TODO: sort
        _nodes = _gameState.GetStartNodeNames(StartNodeType.All).ToList();
        _buttons = _nodes.Select(InitButton).ToList();
    }

    private void UpdateNodes()
    {
        _activeNodes = new(_gameState.GetStartNodeNames(
            _unlockDebugNodes ? StartNodeType.All : StartNodeType.Normal));
        var unlockedAtFirst = new HashSet<string>(_gameState.GetStartNodeNames(
            _unlockAllNodes ? StartNodeType.All : StartNodeType.Unlocked));

        // TODO: add reached history
        _unlockedNodes = new(_activeNodes.Where(unlockedAtFirst.Contains));
    }

    private Button InitButton(string nodeName)
    {
        var button = new Button
        {
            Visible = false,
            Theme = Assets.Instance.DefaultTheme
        };
        button.Pressed += () => StartGame(nodeName);
        _chapterList.AddChild(button);
        return button;
    }

    private void UpdateButtons()
    {
        if (_activeNodes == null)
        {
            return;
        }

        for (var i = 0; i < _nodes.Count; i++)
        {
            var node = _nodes[i];
            var button = _buttons[i];
            if (_activeNodes.Contains(node))
            {
                button.Visible = true;
                if (_unlockedNodes.Contains(node))
                {
                    button.Text = I18n.__(_gameState.GetNode(node, false).DisplayNames);
                    button.Disabled = false;
                }
                else
                {
                    button.Text = I18n.__("title.selectchapter.locked");
                    button.Disabled = true;
                }
            }
            else
            {
                button.Visible = false;
            }
        }
    }

    public void StartGame(string nodeName)
    {
        this.SwitchView<GameViewController>(() => _gameState.StartGame(nodeName));
    }

    public override void ShowPanel(bool doTransition, Action onFinish)
    {
        UpdateNodes();
        if (_unlockedNodes.Count == 0)
        {
            Utils.Warn("Nova: No node is unlocked so the game cannot start. " +
                "Please use is_unlocked_start() rather than is_start() in your first node.");
        }
        else if (_unlockedNodes.Count == 1)
        {
            StartGame(_unlockedNodes.First());
            return;
        }
        UpdateButtons();

        base.ShowPanel(doTransition, onFinish);
    }
}
