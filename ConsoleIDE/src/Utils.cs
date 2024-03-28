using ConsoleIDE.Buttons;

namespace ConsoleIDE;

public static class Utils
{
	public static int GlobalYScroll = 0;

	public static Coordinate GetWindowSize(ScreenReference screen)
	{
		NCurses.GetMaxYX(screen, out int y, out int x);

		return new(x, y);
	}

	public static int GetWindowHeight(ScreenReference screen)
	{
		return GetWindowSize(screen).Y;
	}

	public static int GetWindowWidth(ScreenReference screen)
	{
		return GetWindowSize(screen).X;
	}
}