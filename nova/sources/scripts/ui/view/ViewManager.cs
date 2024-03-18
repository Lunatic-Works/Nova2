using System;
using System.Collections.Generic;

namespace Nova;

public partial class ViewManager : ISingleton
{
    private readonly Dictionary<Type, IPanelController> _controllers = [];

    public void RegisterView(IPanelController controller)
    {
        _controllers.Add(controller.GetType(), controller);
    }

    public T GetController<T>() where T : IPanelController
    {
        return (T)_controllers[typeof(T)];
    }

    public void OnEnter() { }

    public void OnReady()
    {
        GetController<ChapterSelectController>().ShowPanel();
    }

    public void OnExit()
    {
        _controllers.Clear();
    }

    public static ViewManager Instance => NovaController.Instance.GetObj<ViewManager>();
}

public static class ViewHelper
{
    public static void SwitchView<T>(this IPanelController from, Action onFinish = null)
        where T : IPanelController
    {
        var to = ViewManager.Instance.GetController<T>();
        from.HidePanel(() => to.ShowPanel(onFinish));
    }

    public static void HidePanel(this IPanelController panel, Action onFinish)
    {
        panel.HidePanel(true, onFinish);
    }

    public static void ShowPanel(this IPanelController panel, Action onFinish)
    {
        panel.ShowPanel(true, onFinish);
    }

    public static void HidePanel(this IPanelController panel)
    {
        panel.HidePanel(true, null);
    }

    public static void ShowPanel(this IPanelController panel)
    {
        panel.ShowPanel(true, null);
    }

    public static void ShowPanelImmediate(this IPanelController panel, Action onFinish)
    {
        panel.ShowPanel(false, onFinish);
    }

    public static void HidePanelImmediate(this IPanelController panel, Action onFinish)
    {
        panel.HidePanel(false, onFinish);
    }

    public static void ShowPanelImmediate(this IPanelController panel)
    {
        panel.ShowPanel(false, null);
    }

    public static void HidePanelImmediate(this IPanelController panel)
    {
        panel.HidePanel(false, null);
    }
}
