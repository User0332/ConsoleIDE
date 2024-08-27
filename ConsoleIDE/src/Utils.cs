using System.Runtime.InteropServices;
using ConsoleIDE.Buttons;

namespace ConsoleIDE;

public static class Utils
{
	public const int ERR = -1;
	public const uint MOUSE_SCROLL_UP = 2 << ((4 - 1) * 5);
	public const uint MOUSE_SCROLL_DOWN = 2 << ((5 - 1) * 5);
	public static readonly nint CursesLib;

	public delegate int mvchgat_func(int y, int x, int n, uint attr, short color_pair, nint options);

	static readonly mvchgat_func mvchgat;

	static Utils()
	{
		CursesLib = NativeLibrary.Load(new CursesLibraryNames().NamesLinux[1]); // right now only linux support

		mvchgat = (mvchgat_func) Marshal.GetDelegateForFunctionPointer(
			NativeLibrary.GetExport(CursesLib, "mvchgat"),
			typeof(mvchgat_func)
		);
	}

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

	public static void AddStr(Coordinate pos, string str)
	{
		if ((pos.Y >= GetWindowHeight(GlobalScreen.Screen)) || ((pos.X+str.Length) >= GetWindowWidth(GlobalScreen.Screen))) return;

		NCurses.MoveAddString(pos.Y, pos.X, str);
	}

	public static int CTRL(char c)
	{
		return CTRL((int) c);
	}

	public static int CTRL(int c)
	{
		return c & 0x1F;
	}

	public static void MoveChangeAttr(int y, int x, int n, uint attr, short color_pair = 0, nint options = 0)
	{
		int res = mvchgat(y, x, n, attr, color_pair, options);

		if (res == ERR) throw new Exception("MoveChangeAttr (mvchgat) Returned ERR");
	}

	public static bool IsMouseEventType(MouseEvent ev, uint eventType)
	{
		return (ev.bstate & eventType) != 0;
	}

	public static uint COLOR_PAIR(short num)
	{
		return (uint) num << 8;
	}
}

