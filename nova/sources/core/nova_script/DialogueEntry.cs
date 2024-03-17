using System.Collections.Generic;
using System.Linq;
using Godot;

namespace Nova;

public readonly struct LocalizedDialogueEntry
{
    public string DisplayName { get; init; }
    public string Dialogue { get; init; }
}

/// <summary>
/// Dialogue entry without actions. Used for serialization.
/// </summary>
public readonly struct DialogueDisplayData
{
    public Dictionary<string, string> DisplayNames { get; init; }
    public Dictionary<string, string> Dialogues { get; init; }

    public string FormatNameDialogue()
    {
        var name = I18n.__(DisplayNames);
        var dialogue = I18n.__(Dialogues);
        if (string.IsNullOrEmpty(name))
        {
            return dialogue;
        }
        else
        {
            return string.Format(I18n.__("format.namedialogue"), name, dialogue);
        }
    }
}

/// <summary>
/// A dialogue entry contains the character name and the dialogue text in each locale, and the actions to execute.
/// </summary>
public class DialogueEntry(string characterName, string displayName, string dialogue,
    Dictionary<DialogueActionStage, RefCounted> actions, ulong textHash)
{
    /// <summary>
    /// Internally used character name.
    /// </summary>
    public readonly string CharacterName = characterName;

    /// <summary>
    /// Displayed character name in each locale, before string interpolation.
    /// </summary>
    private readonly Dictionary<string, string> _displayNames = new() { [I18n.DefaultLocale] = displayName };

    /// <summary>
    /// Displayed dialogue text in each locale, before string interpolation.
    /// </summary>
    private readonly Dictionary<string, string> _dialogues = new() { [I18n.DefaultLocale] = dialogue };

    /// <summary>
    /// The actions to execute when the game processes to this point.
    /// </summary>
    private readonly Dictionary<DialogueActionStage, RefCounted> _actions = actions;

    public readonly ulong TextHash = textHash;

    public void AddLocalized(string locale, LocalizedDialogueEntry entry)
    {
        _displayNames[locale] = entry.DisplayName;
        _dialogues[locale] = entry.Dialogue;
    }

    // DialogueDisplayData is cached only if there is no need to interpolate
    private DialogueDisplayData? _cachedDisplayData;
    private bool _needInterpolate;

    public bool NeedInterpolate()
    {
        // TODO
        /*
        if (_cachedDisplayData == null && !_needInterpolate)
        {
            var func = LuaRuntime.Instance.GetFunction("text_need_interpolate");
            _needInterpolate = _displayNames.Any(x => func.Invoke<string, bool>(x.Value))
                              || _dialogues.Any(x => func.Invoke<string, bool>(x.Value));
            if (!_needInterpolate)
            {
                _cachedDisplayData = new DialogueDisplayData(_displayNames, _dialogues);
            }
        }

        return _needInterpolate;
        */
        if (_cachedDisplayData == null && !_needInterpolate)
        {
            _needInterpolate = false;
            _cachedDisplayData = new DialogueDisplayData
            {
                DisplayNames = _displayNames,
                Dialogues = _dialogues
            };
        }
        return _needInterpolate;
    }

    public DialogueDisplayData GetDisplayData()
    {
        if (NeedInterpolate())
        {
            return new DialogueDisplayData
            {
                DisplayNames = _displayNames.ToDictionary(x => x.Key, x => InterpolateText(x.Value)),
                Dialogues = _dialogues.ToDictionary(x => x.Key, x => InterpolateText(x.Value))
            };
        }
        return _cachedDisplayData ?? default;
    }

    /// <summary>
    /// Execute the action stored in this dialogue entry.
    /// </summary>
    public void ExecuteAction(DialogueActionStage stage, bool isRestoring)
    {
        if (_actions.TryGetValue(stage, out var action))
        {
            // TODO
        }
    }

    private const string ActionCoroutineName = "__Nova.action_coroutine";

    public static string WrapCoroutine(string code)
    {
        return $@"
{ActionCoroutineName} = coroutine.start(function()
    __Nova.coroutineHelper:AcquireActionPause()
    {code}
    __Nova.coroutineHelper:ReleaseActionPause()
end)";
    }

    public static void StopActionCoroutine()
    {
        // TODO
        // Do nothing if the coroutine is nil
        // LuaRuntime.Instance.GetFunction("coroutine.stop").Call(ActionCoroutineName);
    }

    private static string InterpolateText(string s)
    {
        // TODO
        /*
        if (string.IsNullOrEmpty(s))
        {
            return s;
        }

        return LuaRuntime.Instance.GetFunction("interpolate_text").Invoke<string, string>(s);
        */
        return s;
    }
}
