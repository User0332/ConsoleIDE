namespace ConsoleIDE.Buttons;

public class FolderSelectButton(Coordinate pos, string dir) : IButton
{
	readonly Coordinate startPos = pos;
	readonly ClickableBound bound = new(pos, pos.AddTo(new(dir.Length + 3, 1)));
	readonly string dir = dir;
	public ClickableBound BoundingBox => bound;

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
		Directory.SetCurrentDirectory(dir);
	}

	public void ExecuteSecondaryAction(Coordinate mousePos)
	{
		
	}
}