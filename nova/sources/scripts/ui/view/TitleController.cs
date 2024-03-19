using Godot;

namespace Nova;

public partial class TitleController : PanelController
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
