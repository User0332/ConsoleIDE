using ConsoleIDE.Buttons;
using ConsoleIDE.Delegators;
using EventType = Mindmagma.Curses.CursesMouseEvent;

namespace ConsoleIDE;

static class ClickDelegator
{
	static readonly List<IButton> FrozenClickables = new();
	static readonly List<IButton> CurrentPageClickables = new();
	static ScreenReference screen;

	public static void Init(ScreenReference screen)
	{
		NCurses.Keypad(screen, true);

		NCurses.MouseMask(
			EventType.ALL_MOUSE_EVENTS,
			out _
		);

		Console.WriteLine("\x1b[?1003h"); // xterm: emit all mouse events
		Console.Out.Flush();

		ClickDelegator.screen = screen;
	}

	public static void Quit()
	{
		Console.WriteLine("\x1b[?1003l"); // xterm: stop emitting all mouse events
		Console.Out.Flush();
	}

	public static void Delegate(MouseEvent ev)
	{
		NCurses.Erase();

		if (
			((ev.bstate & EventType.BUTTON1_CLICKED) == 0) &&
			((ev.bstate & EventType.BUTTON2_CLICKED) == 0) &&
			((ev.bstate & EventType.REPORT_MOUSE_POSITION) == 0)
		)
		{
			ViewDelegator.ProcessMouse(ev);
		}

		foreach (IButton btn in CurrentPageClickables.Concat(FrozenClickables).ToArray()) // .ToArray() to copy in case collection is modified during iter
		{
			btn.Render(screen);

			if (!btn.BoundingBox.Contains(ev.x, ev.y)) continue;

			
			if ((ev.bstate & EventType.BUTTON1_CLICKED) != 0)
			{
				btn.ExecuteAction(new(ev.x, ev.y));
			}
			else if ((ev.bstate & EventType.BUTTON2_CLICKED) != 0)
			{
				btn.ExecuteSecondaryAction(new(ev.x, ev.y));
			}
			else if ((ev.bstate & EventType.REPORT_MOUSE_POSITION) != 0)
			{
				btn.HoverUpdate(screen);
			}
		}
	}
	
	public static void Register(IButton clickable)
	{
		CurrentPageClickables.Add(clickable);
	}

	public static void RegisterFrozen(IButton clickable)
	{
		FrozenClickables.Add(clickable);
	}

	public static void Clear()
	{
		NCurses.Erase();

		ClearKeepScreen();
	}

	public static void ClearKeepScreen()
	{
		CurrentPageClickables.Clear();
	}

	public static void ClearFrozen()
	{
		NCurses.Erase();

		FrozenClickables.Clear();
	}
}