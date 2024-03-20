using Godot;

namespace Nova;

public class Assets : ISingleton
{
    public const string ResourceRoot = "res://resources/";
    public const string NovaResourceRoot = "res://nova/resources/";

    public Theme DefaultTheme { get; private set; }

    public void OnEnter()
    {
        DefaultTheme = ResourceLoader.Load<Theme>(NovaResourceRoot + "default_theme.tres");
    }

    public void OnReady() { }

    public void OnExit() { }

    public static Assets Instance => NovaController.Instance.GetObj<Assets>();
}
