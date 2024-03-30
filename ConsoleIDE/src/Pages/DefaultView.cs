using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages;

public class DefaultView(ScreenReference screen) : IView
{
	readonly ScreenReference screen = screen;

	public void InitFrozens()
	{
		ClickDelegator.RegisterFrozen(new ExitButton());
		ClickDelegator.RegisterFrozen(
			new SelectFolderView.GotoButton(
				new(0, 6)
			)
		);
	}
	
	public void Update(ScreenReference screen)
	{
		NCurses.MoveAddString(0, 0, @"====  =====  |\  |  /¯¯  ===== |    |===     === |==  |===    ||    ====  __/__/_");
		NCurses.MoveAddString(1, 0, @"|     |   |  | \ |  \__  |   | |    |==       |  |  | |==     ||    |    __/__/_");
		NCurses.MoveAddString(2, 0, @"====  =====  |  \|  __/  ===== |___ |===     === |==  |===    ||    ====  /  /");

		NCurses.MoveAddString(4, 0, new string('=', Utils.GetWindowWidth(screen)));
	}
}