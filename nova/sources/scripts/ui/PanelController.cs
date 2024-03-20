using System;
using Godot;

namespace Nova;

public partial class PanelController : Control, IPanelController
{
    public bool Active => Visible;

    protected virtual void OnTransitionBegin() { }

    protected virtual void OnShowFinish() { }

    // this function calls before myPanel inactive
    protected virtual void OnHideComplete() { }

    // this function calls after myPanel inactive but before onFinish
    protected virtual void OnHideFinish() { }

    public virtual void ShowPanel(bool doTransition, Action onFinish)
    {
        if (Active)
        {
            onFinish?.Invoke();
            return;
        }

        Visible = true;
        // TODO: transition
        OnShowFinish();
        onFinish?.Invoke();
    }

    public virtual void HidePanel(bool doTransition, Action onFinish)
    {
        if (!Active)
        {
            onFinish?.Invoke();
            return;
        }

        // TODO: transition
        OnHideComplete();
        Visible = false;
        OnHideFinish();
        onFinish?.Invoke();
    }
}
