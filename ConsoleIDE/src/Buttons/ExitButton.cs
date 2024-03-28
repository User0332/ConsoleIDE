namespace ConsoleIDE.Buttons;

public class ExitButton : IButton
{
	readonly Coordinate startPos;
	readonly ClickableBound bound;

	public static readonly Coordinate Size = new(3, 1);
	public ClickableBound BoundingBox => bound;

	public ExitButton(Coordinate pos)
	{
		startPos = pos;	
		bound = new(pos, pos.AddTo(Size));
	}
	
	public ExitButton() : this(new(0, 0)) {}

	public void Render(ScreenReference screen)
	{
		NCurses.MoveAddString(startPos.Y, startPos.X, "[X]");
	}
	
	public void HoverUpdate(ScreenReference screen)
	{
		
	}

	public void ExecuteAction(Coordinate mousePos)
	{
		NCurses.EndWin();
		ClickDelegator.Quit();
		Environment.Exit(0);
	}

	public void ExecuteSecondaryAction(Coordinate mousePos)
	{
		
	}
}