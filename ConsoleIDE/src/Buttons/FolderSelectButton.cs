namespace ConsoleIDE.Buttons;

public class FolderSelectButton : IButton
{
	readonly Coordinate startPos;
	readonly ClickableBound bound;
	readonly string dir;
	public ClickableBound BoundingBox => bound;

	public FolderSelectButton(Coordinate pos, string dir)
	{
		startPos = pos;	
		bound = new(pos, pos.AddTo(new(dir.Length+3, 1)));
		this.dir = dir;
	}
	
	public FolderSelectButton(string dir) : this(new(0, 0), dir) {}

	public void Render(ScreenReference screen)
	{
		NCurses.MoveAddString(startPos.Y, startPos.X, $"[{dir}/]");
	}
	
	public void HoverUpdate(ScreenReference screen)
	{
		
	}

	public void ExecuteAction(Coordinate mousePos)
	{
		ClickDelegator.Clear();

		// if (Directory.GetCurrentDirectory().Contains("bin")) throw new Exception();

		// File.AppendAllText("./debug.txt", $"{Directory.GetCurrentDirectory()}, {dir}\n");
		


		Directory.SetCurrentDirectory(dir);
	}

	public void ExecuteSecondaryAction(Coordinate mousePos)
	{
		
	}
}