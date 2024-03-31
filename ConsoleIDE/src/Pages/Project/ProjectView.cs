using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class ProjectView : IView
{
	readonly ScreenReference screen;
	readonly FileView fileEditor;
	readonly DirectoryView fileExplorer;

	public ProjectView(ScreenReference screen, string projectDir)
	{
		this.screen = screen;
		fileEditor = new(new(30, 0), Utils.GetWindowWidth(screen));;
		fileExplorer = new(new(0, 0), projectDir, 28, screen, fileEditor.ChangeTo);
	}

	public void InitFrozens()
	{
		NCurses.SetCursor(0);
		ClickDelegator.RegisterFrozen(new ExitButton());
		ClickDelegator.RegisterFrozen(new BackButton());
	}
	
	public void Update(ScreenReference screen)
	{
		ClickDelegator.ClearKeepScreen();

		fileExplorer.Render();
		fileEditor.Render();

	}

	public void RecieveKey(int key)
	{
		fileEditor.SendKey((char) key);
	}

	public void RecieveMouseInput(MouseEvent ev)
	{
		fileEditor.SendMouseEvent(ev);
	}
}