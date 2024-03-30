global using ScreenReference = nint;
global using Mindmagma.Curses;

using ConsoleIDE.Delegators;

namespace ConsoleIDE;

class IDEMain
{
	static int Main(string[] args)
	{
		ScreenReference screen = NCurses.InitScreen();

		GlobalScreen.Screen = screen;

		NCurses.StartColor();
		NCurses.ScrollOk(screen, true);
		NCurses.NoDelay(screen, true);
		NCurses.NoEcho(); 

		ClickDelegator.Init(screen);
		ViewDelegator.Init(screen);

		Console.CancelKeyPress += (object? sender, ConsoleCancelEventArgs e) => {
			ClickDelegator.Quit();
		};

		NCurses.NoEcho();
		// NCurses.Raw();

		MouseEvent mouseEvent = new()
		{
			bstate = CursesMouseEvent.REPORT_MOUSE_POSITION,
			x = 0,
			y = 0
		};

		while (true)
		{
			// NCurses.Clear();

			switch (NCurses.GetChar())
			{
				case -1: break;
				case CursesKey.MOUSE:
					try {
						NCurses.GetMouse(out MouseEvent mouseEv);
						ClickDelegator.Delegate(mouseEv);

						// don't double/multi-count mouse clicks, only multi-report mouse pos
						
						if (mouseEv.bstate == CursesMouseEvent.REPORT_MOUSE_POSITION)
						{
							mouseEvent = mouseEv; // set the loop mouse event
						}
						
					}
					catch (DotnetCursesException) { /* no events to catch */ }
					break;
			}

			ClickDelegator.Delegate(mouseEvent);
			ViewDelegator.Render(screen);

			NCurses.Refresh();
		}
	}
}