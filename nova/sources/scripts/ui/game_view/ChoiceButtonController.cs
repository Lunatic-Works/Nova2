using System;
using System.Collections.Generic;
using Godot;

namespace Nova;

public partial class ChoiceButtonController : Button
{
    public IReadOnlyDictionary<string, string> DisplayTexts { get; private set; }

    private Action _onClick;

    public override void _EnterTree()
    {
        I18n.Instance.LocaleChanged.Subscribe(UpdateText);
        Pressed += _onClick;
    }

    public override void _Ready()
    {
        UpdateText();
    }

    public override void _ExitTree()
    {
        I18n.Instance.LocaleChanged.Unsubscribe(UpdateText);
        Pressed -= _onClick;
        // clear reference
        DisplayTexts = null;
        _onClick = null;
    }

    public void Init(ChoiceData choiceData, Action onClick)
    {
        Theme = Assets.Instance.DefaultTheme;
        DisplayTexts = choiceData.Texts;
        _onClick = onClick;
    }

    public void UpdateText()
    {
        if (!IsNodeReady())
        {
            return;
        }
        Text = I18n.__(DisplayTexts);
    }
}
