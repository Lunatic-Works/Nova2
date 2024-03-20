using Godot;

namespace Nova;

public partial class TitleController : ViewController
{
    public void OnStartGame()
    {
        this.SwitchView<ChapterSelectController>();
    }

    public static void OnQuit()
    {
        Utils.Quit();
    }
}
