using ConsoleIDE.Buttons;
using ConsoleIDE.Delegators;
using ConsoleIDE.Pages.Project;

namespace ConsoleIDE.Pages;

public class SelectFolderView(ScreenReference screen) : IView
{
	readonly ScreenReference screen = screen;

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
		

		if (Utils.GlobalYScroll < dirs.Count)
		{
			for (int i = Utils.GlobalYScroll; i < Math.Min(Utils.GlobalYScroll+Utils.GetWindowHeight(screen)-2, dirs.Count); i++)
			{
				ClickDelegator.Register(
					new FolderSelectButton(
						new(0, i-Utils.GlobalYScroll+2), dirs[i]
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