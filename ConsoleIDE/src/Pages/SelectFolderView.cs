using ConsoleIDE.Buttons;
using ConsoleIDE.Delegators;
using ConsoleIDE.Pages.Project;

namespace ConsoleIDE.Pages;

public class SelectFolderView(ScreenReference screen) : IView
{
	readonly ScreenReference screen = screen;

	int yScroll;

	public void InitFrozens()
	{
		ClickDelegator.RegisterFrozen(new ExitButton());
		ClickDelegator.RegisterFrozen(new BackButton());
	}

	public void Update(nint screen)
	{
		ClickDelegator.ClearKeepScreen();

		var dirs = Directory.GetDirectories("./").ToList();
		
		dirs.Add("..");
		

		if (yScroll < dirs.Count)
		{
			for (int i = yScroll; i < Math.Min(yScroll+Utils.GetWindowHeight(screen)-2, dirs.Count); i++)
			{
				ClickDelegator.Register(
					new FolderSelectButton(
						new(0, i-yScroll+2), dirs[i]
					)
				);
			}
		}

		ClickDelegator.Register(
			new TextButton(
				new(0, 0),
				$"Select Current Folder ({Directory.GetCurrentDirectory()})",
				(mousePos) => {
					ViewDelegator.Push(new ProjectView(screen, Directory.GetCurrentDirectory()));
				}
			)
		);
	}

	public void RecieveKey(int key)
	{

	}

	public void RecieveMouseInput(MouseEvent ev)
	{
		if (Utils.IsMouseEventType(ev, Utils.MOUSE_SCROLL_UP))
		{
			yScroll = Math.Max(0, yScroll-1);

			return;
		}
		else if (Utils.IsMouseEventType(ev, Utils.MOUSE_SCROLL_DOWN))
		{
			yScroll = Math.Min(Math.Max(0, Directory.GetDirectories("./").Length-Utils.GetWindowHeight(screen)), yScroll+1);
		
			return;
		}
	}

	public class GotoButton(Coordinate pos) : IButton
	{
		readonly Coordinate startPos = pos;
		readonly ClickableBound bound = new(pos, pos.AddTo(Size));
		
		public static readonly Coordinate Size = new(23, 1);
		public ClickableBound BoundingBox => bound;

		public GotoButton() : this(new(0, 0)) {}

		public void Render(ScreenReference screen)
		{
			NCurses.MoveAddString(startPos.Y, startPos.X, "[Select Project Folder]");
		}
		
		public void HoverUpdate(ScreenReference screen)
		{
			
		}

		public void ExecuteAction(Coordinate mousePos)
		{
			ViewDelegator.Push(new SelectFolderView(GlobalScreen.Screen));
		}

		public void ExecuteSecondaryAction(Coordinate mousePos)
		{
			
		}
	}
}