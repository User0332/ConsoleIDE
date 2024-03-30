using ConsoleIDE.Delegators;

namespace ConsoleIDE.Buttons;

public class BackButton(Coordinate pos) : IButton
{
	readonly Coordinate startPos = pos;
	readonly ClickableBound bound = new(pos, pos.AddTo(Size));

	public static readonly Coordinate Size = new(5, 1);
	public ClickableBound BoundingBox => bound;

	public BackButton() : this(new(Utils.GetWindowWidth(GlobalScreen.Screen)-ExitButton.Size.X-1-Size.X, 0)) {}

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