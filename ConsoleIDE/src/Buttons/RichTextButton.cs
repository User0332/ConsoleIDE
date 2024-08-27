namespace ConsoleIDE.Buttons;

public class RichTextButton(Coordinate pos, string text, Action<Coordinate> action, Action<Coordinate> secondaryAction, uint attrs, short colorPairs = 0, nint options = 0) : IButton
{
	readonly Coordinate startPos = pos;
	readonly ClickableBound bound = new(pos, pos.AddTo(new(text.Length, 1)));
	readonly Action<Coordinate> action = action;
	readonly Action<Coordinate> secondaryAction = secondaryAction;
	readonly string text = text;
	readonly uint attrs = attrs;
	readonly short colorPairs = colorPairs;
	readonly nint options = options;

	public ClickableBound BoundingBox => bound;

	public RichTextButton(Coordinate pos, string text, Action<Coordinate> action, uint attrs, short colorPairs = 0, nint options = 0)
		: this(pos, text, action, (coord) => {}, attrs, colorPairs, options) {}

	public void Render(ScreenReference screen)
	{
		NCurses.MoveAddString(startPos.Y, startPos.X, text);
		Utils.MoveChangeAttr(startPos.Y, startPos.X, text.Length, attrs, colorPairs, options);
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