using System;
using Godot;

namespace Nova;

public partial class ChoicesController : PanelController
{
    private NodePool<ChoiceButtonController> _pool;
    private GameState _gameState;

    public override void _EnterTree()
    {
        _pool = new(() => new ChoiceButtonController());
        _gameState = GameState.Instance;
        _gameState.ChoiceOccurs.Subscribe(RaiseChoices);
    }

    private void RaiseChoices(ChoiceOccursData data)
    {
        if (data.Choices.Count == 0)
        {
            throw new ArgumentException("Nova: No active selection.");
        }

        Visible = true;
        for (var i = 0; i < data.Choices.Count; i++)
        {
            var choice = data.Choices[i];
            var index = i;
            var button = _pool.Get(this, e => e.Init(choice, () => Select(index)));
        }
    }

    private void ClearChoices()
    {
        Visible = false;
        foreach (var child in GetChildren())
        {
            if (child is ChoiceButtonController button)
            {
                _pool.Put(button, this);
            }
        }
    }

    private void Select(int index)
    {
        ClearChoices();
        _gameState.SignalFence(index);
    }
}
