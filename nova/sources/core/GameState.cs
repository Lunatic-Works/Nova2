using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Nova;

public partial class GameState : ISingleton
{
    private FlowChartGraph _flowChartGraph;

    public FlowChartNode CurrentNode { get; private set; }
    private int _currentDialogueIndex;
    private DialogueEntry _currentDialogueEntry;
    private bool _ensureCheckpoint;

    private enum State
    {
        Normal,
        Ended,
        Restoring,
        Upgrading
    }
    private State _state = State.Normal;
    public bool IsEnded => _state == State.Ended;
    public bool IsRestoring => _state == State.Restoring;
    public bool IsUpgrading => _state == State.Upgrading;
    public bool CanStepForward => CurrentNode != null && _state == State.Normal && !_fence.Taken;

    /// <summary>
    /// The current coroutine for async functions.
    /// </summary>
    private Coroutine _coroutine;
    /// <summary>
    /// The fence used to switch context back to game and get result from user interaction. (i.e. a pause)
    /// </summary>
    private readonly Fence _fence = new();

    #region Events

    public readonly Event GameStarted = new();
    /// <summary>
    /// This event will be triggered if the node has changed. The new node name will be sent to all listeners.
    /// </summary>
    public readonly Event<NodeChangedData> NodeChanged = new();
    /// <summary>
    /// This event will be triggered if the content of the dialogue will change. It will be triggered before
    /// the lazy execution block of the new dialogue is invoked.
    /// </summary>
    public readonly Event DialogueWillChange = new();
    /// <summary>
    /// This event will be triggered if the content of the dialogue has changed. The new dialogue text will be
    /// sent to all listeners.
    /// </summary>
    public readonly Event<DialogueChangedData> DialogueChangedEarly = new();
    public readonly Event<DialogueChangedData> DialogueChanged = new();
    /// <summary>
    /// This event will be triggered if choices occur, either when branches occur or when choices are
    /// triggered from the script.
    /// </summary>
    public readonly Event<ChoiceOccursData> ChoiceOccurs = new();
    /// <summary>
    /// This event will be triggered if the story route has reached an end.
    /// </summary>
    public readonly Event<ReachedEndData> RouteEnded = new();
    public readonly Event<bool> RestoreStarts = new();

    #endregion

    public void OnEnter()
    {
        _flowChartGraph = NovaController.Instance.GetObj<ScriptLoader>().FlowChartGraph;
    }

    public void OnReady() { }

    public void OnExit()
    {
        CancelCoroutine();
    }

    private void StartCoroutine(Func<CancellationToken, Task> asyncFunc)
    {
        ResetCoroutineContext();
        _coroutine = Coroutine.Start(asyncFunc);
    }

    private void CancelCoroutine()
    {
        _coroutine?.Cancel();
        _coroutine = null;
        ResetCoroutineContext();
    }

    private void ResetCoroutineContext()
    {
        // TODO
    }

    public void ResetGameState()
    {
        CancelCoroutine();
        CurrentNode = null;
        _currentDialogueIndex = 0;
        _currentDialogueEntry = null;
        _state = State.Ended;
    }

    public void SignalFence<T>(T result)
    {
        _fence.Signal(result);
    }

    public FlowChartNode GetNode(string name, bool addDeferred = true)
    {
        var node = _flowChartGraph.GetNode(name);
        if (addDeferred)
        {
            ScriptLoader.AddDeferredDialogueChunks(node);
        }
        return node;
    }

    public IEnumerable<string> GetStartNodeNames(StartNodeType type)
    {
        return _flowChartGraph.GetStartNodeNames(type);
    }

    private void StartGame(FlowChartNode startNode)
    {
        ResetGameState();
        _state = State.Normal;
        GameStarted.Invoke();
        MoveToNextNode(startNode);
    }

    public void StartGame(string startNode)
    {
        StartGame(GetNode(startNode));
    }

    public void Step()
    {
        if (!CanStepForward)
        {
            return;
        }

        if (_currentDialogueIndex + 1 < CurrentNode.DialogueEntryCount)
        {
            ++_currentDialogueIndex;
            UpdateGameState(false, true, false, true, false);
        }
        else
        {
            StepAtEndOfNode();
        }
    }

    /// <summary>
    /// Called after the current node or the current dialogue index has changed
    /// </summary>
    /// <remarks>
    /// Trigger events according to the current states and how they were changed
    /// </remarks>
    private void UpdateGameState(bool nodeChanged, bool dialogueChanged, bool firstEntryOfNode,
        bool dialogueStepped, bool fromCheckpoint)
    {
        if (nodeChanged)
        {
            NodeChanged.Invoke(new() { NewNode = CurrentNode.Name });
            if (firstEntryOfNode)
            {
                _ensureCheckpoint = true;
            }
        }

        if (dialogueChanged)
        {
            Utils.RuntimeAssert(_currentDialogueIndex >= 0 && (
                CurrentNode.DialogueEntryCount == 0 ||
                _currentDialogueIndex < CurrentNode.DialogueEntryCount),
                "Dialogue index out of range.");

            if (CurrentNode.DialogueEntryCount > 0)
            {
                _currentDialogueEntry = CurrentNode.GetDialogueEntryAt(_currentDialogueIndex);
                StartCoroutine(token => UpdateDialogue(firstEntryOfNode, dialogueStepped, fromCheckpoint, token));
            }
            else
            {
                StepAtEndOfNode();
            }
        }
    }

    private async Task ExecuteDialogueAction(DialogueActionStage stage, CancellationToken token)
    {
        _currentDialogueEntry.ExecuteAction(stage, IsRestoring);
        await _fence.Barrier(token);
    }

    private async Task UpdateDialogue(bool firstEntryOfNode, bool dialogueStepped,
        bool fromCheckpoint, CancellationToken token)
    {
        // 1. execute BeforeCheckpoint action
        if (!fromCheckpoint)
        {
            await ExecuteDialogueAction(DialogueActionStage.BeforeCheckpoint, token);
        }

        // 2. save Checkpoint
        var isReached = SaveCheckpoint(firstEntryOfNode, dialogueStepped);

        // 3. invoke will change event
        DialogueWillChange.Invoke();

        // 3. execute Default action
        await ExecuteDialogueAction(DialogueActionStage.Default, token);

        // 4. save reached data
        var isReachedAnyHistory = SaveReachedData(out var dialogueData);
        var dialogueChangedData = new DialogueChangedData()
        {
            DialogueData = dialogueData,
            DisplayData = _currentDialogueEntry.GetDisplayData(),
            IsReached = isReached, IsReachedAnyHistory = isReachedAnyHistory
        };

        // 5. invoke early event and then default event
        DialogueChangedEarly.Invoke(dialogueChangedData);
        DialogueChanged.Invoke(dialogueChangedData);

        // 6. execute AfterDialogue action
        await ExecuteDialogueAction(DialogueActionStage.AfterDialogue, token);

        // TODO: advancedDialogueHelper
    }

    private void StepAtEndOfNode()
    {
        switch (CurrentNode.Type)
        {
            case FlowChartNodeType.Normal:
                MoveToNextNode(CurrentNode.Next);
                break;
            case FlowChartNodeType.Branching:
                StartCoroutine(token => DoBranch(CurrentNode.GetAllBranches(), token));
                break;
            case FlowChartNodeType.End:
                _state = State.Ended;
                var endName = _flowChartGraph.GetEndName(CurrentNode);
                // TODO
                // checkpointManager.SetEndReached(endName);
                RouteEnded.Invoke(new() { EndName = endName });
                break;
            default:
                throw new ArgumentOutOfRangeException();
        }
    }

    private void MoveToNextNode(FlowChartNode nextNode)
    {
        ScriptLoader.AddDeferredDialogueChunks(nextNode);
        // in case of empty node, do not change any of these
        // so the bookmark is left at the end of last node
        if (nextNode.DialogueEntryCount > 0)
        {
            // TODO
            // nodeRecord = checkpointManager.GetNextNode(nodeRecord, nextNode.name, variables, 0);
            _currentDialogueIndex = 0;
            // checkpointOffset = nodeRecord.offset;
        }

        CurrentNode = nextNode;
        UpdateGameState(true, true, true, true, false);
    }

    private async Task DoBranch(IEnumerable<BranchInformation> branchInfos, CancellationToken token)
    {
        var choices = new List<ChoiceData>();
        var choiceNames = new List<string>();
        foreach (var branchInfo in branchInfos)
        {
            if (branchInfo.Mode == BranchMode.Jump)
            {
                if (GDRuntime.InvokeCondition(branchInfo.Condition))
                {
                    SelectBranch(branchInfo.Name);
                    return;
                }
                continue;
            }

            if (branchInfo.Mode == BranchMode.Show && !GDRuntime.InvokeCondition(branchInfo.Condition))
            {
                continue;
            }

            var choice = new ChoiceData()
            {
                Texts = branchInfo.Texts, ImageInfo = branchInfo.ImageInfo,
                Interactable = branchInfo.Mode != BranchMode.Enable || GDRuntime.InvokeCondition(branchInfo.Condition)
            };
            choices.Add(choice);
            choiceNames.Add(branchInfo.Name);
        }

        var fence = _fence.Take<int>(token);
        RaiseChoices(choices);
        var index = await fence;

        SelectBranch(choiceNames[index]);
    }

    public void RaiseChoices(IReadOnlyList<ChoiceData> choices)
    {
        ChoiceOccurs.Invoke(new() { Choices = choices });
    }

    private void SelectBranch(string branchName)
    {
        MoveToNextNode(CurrentNode.GetNext(branchName));
    }

    private bool SaveCheckpoint(bool firstEntryOfNode, bool dialogueStepped)
    {
        // TODO
        return false;
    }

    private bool SaveReachedData(out ReachedDialogueData dialogueData)
    {
        dialogueData = new ReachedDialogueData()
        {
            NodeName = CurrentNode.Name,
            DialogueIndex = _currentDialogueIndex,
            NeedInterpolate = _currentDialogueEntry.NeedInterpolate,
            TextHash = _currentDialogueEntry.TextHash
        };
        // TODO
        return false;
    }

    public static GameState Instance => NovaController.Instance.GetObj<GameState>();
}
