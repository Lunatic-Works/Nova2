using System;

namespace Nova;

public interface IPanelController
{
    public bool Active { get; }
    void ShowPanel(bool doTransition, Action onFinish);
    void HidePanel(bool doTransition, Action onFinish);
}
