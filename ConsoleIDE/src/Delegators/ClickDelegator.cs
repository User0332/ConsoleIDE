using ConsoleIDE.Buttons;

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
			EventType.BUTTON1_CLICKED |
			EventType.BUTTON2_CLICKED |
			EventType.REPORT_MOUSE_POSITION,
			out _
		);

		Console.WriteLine("\x1b[?1003h");
		Console.Out.Flush();

		ClickDelegator.screen = screen;
	}

	public static void Quit()
	{
		Console.WriteLine("\x1b[?1003l");
		Console.Out.Flush();
	}

	public static void Delegate(MouseEvent ev)
	{
		try
		{
			foreach (IButton btn in CurrentPageClickables.Concat(FrozenClickables))
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
		} catch (InvalidOperationException) {} // if this is changed during iteration, it was cleared so don't bother going through the rest of the buttons
	}
	
	public static void Register(IButton clickable)
	{
		if (CurrentPageClickables.Contains(clickable)) return;
		
		CurrentPageClickables.Add(clickable);
	}

	public static void RegisterFrozen(IButton clickable)
	{
		FrozenClickables.Add(clickable);
	}

	public static void Clear()
	{
		NCurses.Clear();
		CurrentPageClickables.Clear();
	}

	public static void ClearFrozen()
	{
		NCurses.Clear();
		FrozenClickables.Clear();
	}
}