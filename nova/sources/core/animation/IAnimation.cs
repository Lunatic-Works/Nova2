using Godot;

namespace Nova;

public interface IAnimation
{
    /// <returns>Whether children should be executed.</returns>
    bool Execute(Tween tween);
    /// <returns>Whether children should be executed.</returns>
    bool ExecuteImmediate();
}
