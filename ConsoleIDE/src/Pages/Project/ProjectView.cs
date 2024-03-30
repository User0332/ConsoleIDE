using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class ProjectView : IView
{
	readonly ScreenReference screen;
	readonly FileView fileEditor = new(new(50, 0), 10);
	readonly DirectoryView fileExplorer;

	public ProjectView(ScreenReference screen, string projectDir)
	{
		this.screen = screen;
		fileExplorer = new(new(0, 0), projectDir, Utils.GetWindowWidth(screen), screen, fileEditor.ChangeTo);
	}

	public void InitFrozens()
	{
		ClickDelegator.RegisterFrozen(new ExitButton());
		ClickDelegator.RegisterFrozen(new BackButton());
	}
	
	public void Update(ScreenReference screen)
	{
		ClickDelegator.ClearKeepScreen();

		fileExplorer.Render();
		fileEditor.Render();

	}
}