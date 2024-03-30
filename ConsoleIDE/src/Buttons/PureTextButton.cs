namespace ConsoleIDE.Buttons;

public class PureTextButton(Coordinate pos, string text, Action<Coordinate> action, Action<Coordinate> secondaryAction) : IButton
{
	readonly Coordinate startPos = pos;
	readonly ClickableBound bound = new(pos, pos.AddTo(new(text.Length, 1)));
	readonly Action<Coordinate> action = action;
	readonly Action<Coordinate> secondaryAction = secondaryAction;
	readonly string text = text;
	public ClickableBound BoundingBox => bound;

	public PureTextButton(Coordinate pos, string text, Action<Coordinate> action)
		: this(pos, text, action, (coord) => {}) {}

	public void Render(ScreenReference screen)
	{
		NCurses.MoveAddString(startPos.Y, startPos.X, text);
	}
	
	public void HoverUpdate(ScreenReference screen)
	{
		
	}

	public void ExecuteAction(Coordinate mousePos)
	{
		action(mousePos);
	}

	public void ExecuteSecondaryAction(Coordinate mousePos)
	{
		secondaryAction(mousePos);
	}
}