using System.Collections.Generic;
using Godot;

namespace Nova;

public class StateManager : ISingleton
{
    private GameState _gameState;

    private readonly List<IStateObject> _states = [];

    public AnimationState Animation { get; private set; }

    public void OnEnter()
    {
        _gameState = GameState.Instance;
        _gameState.DialogueChangedEarly.Subscribe(_ => OnDialogueChangedEarly());

        var objectManager = ObjectManager.Instance;

        Animation = new AnimationState();
        _states.Add(Animation);
        objectManager.BindObject("anim", Animation.Root);
    }

    public void OnExit() { }

    public void OnReady() { }

    private void OnDialogueChangedEarly()
    {
        if (_gameState.IsRestoring)
        {
            foreach (var state in _states)
            {
                state.SyncImmediate();
            }
        }
        else
        {
            foreach (var state in _states)
            {
                state.Sync();
            }
        }
    }

    public static StateManager Instance => NovaController.Instance.GetObj<StateManager>();
}
