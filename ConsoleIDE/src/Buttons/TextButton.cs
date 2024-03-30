namespace ConsoleIDE.Buttons;

public class TextButton : IButton
{
	readonly Coordinate startPos;
	readonly ClickableBound bound;
	readonly Action<Coordinate> action;
	readonly Action<Coordinate> secondaryAction;
	readonly string text;
	public ClickableBound BoundingBox => bound;

	public TextButton(Coordinate pos, string text, Action<Coordinate> action, Action<Coordinate> secondaryAction)
	{
		startPos = pos;	
		bound = new(pos, pos.AddTo(new(text.Length+2, 1)));
		this.text = $"[{text}]";
		this.action = action;
		this.secondaryAction = secondaryAction;
	}
	
	public TextButton(Coordinate pos, string text, Action<Coordinate> action)
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