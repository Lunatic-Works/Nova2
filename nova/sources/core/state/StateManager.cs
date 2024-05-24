using System.Collections.Generic;
using Godot;

namespace Nova;

public class StateManager : ISingleton
{
    private GameState _gameState;
    private ObjectManager _objectManager;

    private readonly List<IStateObject> _states = [];

    public AnimationState Animation { get; private set; }

    public void OnEnter()
    {
        _gameState = GameState.Instance;
        _gameState.DialogueChangedEarly.Subscribe(_ => OnDialogueChangedEarly());

        _objectManager = ObjectManager.Instance;
        Animation = new AnimationState();
        _states.Add(Animation);
        _objectManager.BindObject("anim", Animation.Root);
    }

    public void OnExit() { }

    public void OnReady() { }

    public void SyncBackend()
    {
        foreach (var state in _states)
        {
            state.SyncBackend();
        }
    }

    public void SyncImmediate()
    {
        foreach (var state in _states)
        {
            state.SyncImmediate();
        }
    }

    public void Sync()
    {
        foreach (var state in _states)
        {
            state.Sync();
        }
    }

    private void OnDialogueChangedEarly()
    {
        if (_gameState.IsRestoring || _gameState.IsUpgrading)
        {
            SyncBackend();
        }
        else
        {
            Sync();
        }
    }

    public void BindPropertyState(string name, PropertyState state)
    {
        _states.Add(state);
        _objectManager.BindObject(name, state);
    }

    public static StateManager Instance => NovaController.Instance.GetObj<StateManager>();
}
