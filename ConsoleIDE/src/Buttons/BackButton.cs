using ConsoleIDE.Delegators;

namespace ConsoleIDE.Buttons;

public class BackButton : IButton
{
	readonly Coordinate startPos;
	readonly ClickableBound bound;

	public static readonly Coordinate Size = new(5, 1);
	public ClickableBound BoundingBox => bound;

	public BackButton(Coordinate pos)
	{
		startPos = pos;	
		bound = new(pos, pos.AddTo(Size));
	}
	
	public BackButton() : this(new(0, 0)) {}

	public void Render(ScreenReference screen)
	{
		NCurses.MoveAddString(startPos.Y, startPos.X, "[<--]");
	}
	
	public void HoverUpdate(ScreenReference screen)
	{
		
	}

	public void ExecuteAction(Coordinate mousePos)
	{
		ViewDelegator.Pop();
	}

	public void ExecuteSecondaryAction(Coordinate mousePos)
	{
		
	}
}