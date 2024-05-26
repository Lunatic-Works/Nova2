using Godot;

namespace Nova;

public interface IAnimation
{
    /// <summary>
    /// Called when animation entry is created.
    /// </summary>
    void Init() { }
    /// <returns>Whether children should be executed.</returns>
    bool Execute(Tween tween);
}
