using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages;

public class ProjectView : IView
{
	readonly ScreenReference screen;
	readonly string projectDir;
	readonly string dirDispName;

	public ProjectView(ScreenReference screen, string projectDir)
	{
		this.screen = screen;
		this.projectDir = projectDir;
		
		dirDispName = $"{new DirectoryInfo(projectDir).Name}/";
	}

	public void InitFrozens()
	{
		ClickDelegator.RegisterFrozen(new ExitButton());
		ClickDelegator.RegisterFrozen(new BackButton());
	}
	
	public void Update(ScreenReference screen)
	{
		NCurses.MoveAddString(0, 0, dirDispName);
	}
}