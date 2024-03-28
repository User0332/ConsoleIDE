using ConsoleIDE.Buttons;
using ConsoleIDE.Delegators;

namespace ConsoleIDE.Pages;

public class SelectFolderView : IView
{
	public class GotoButton : IButton
	{
		readonly Coordinate startPos;
		readonly ClickableBound bound;
		
		public static readonly Coordinate Size = new(23, 1);
		public ClickableBound BoundingBox => bound;

		public GotoButton(Coordinate pos)
		{
			startPos = pos;	
			bound = new(pos, pos.AddTo(Size));
		}
		
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

	readonly ScreenReference screen;

	public SelectFolderView(ScreenReference screen)
	{
		this.screen = screen;
	}

	public void InitFrozens()
	{
		ExitButton exitBtn = new(new(Utils.GetWindowWidth(screen)-ExitButton.Size.X, 0));
		BackButton backBtn = new(new(Utils.GetWindowWidth(screen)-ExitButton.Size.X-1-BackButton.Size.X, 0));
		
		ClickDelegator.RegisterFrozen(exitBtn);
		ClickDelegator.RegisterFrozen(backBtn);
	}

	public void Update(nint screen)
	{
		var dirs = Directory.GetDirectories("./").ToList();
		
		dirs.Add("..");
		

		if (Utils.GlobalYScroll < dirs.Count)
		{
			for (int i = Utils.GlobalYScroll; i < Math.Min(Utils.GlobalYScroll+Utils.GetWindowHeight(screen), dirs.Count); i++)
			{
				ClickDelegator.Register(
					new FolderSelectButton(
						new(0, i), dirs[i]
					)
				);
			}
		}
	}
}