namespace ConsoleIDE.Buttons;

public interface IButton
{
	public ClickableBound BoundingBox { get; }
	void Render(ScreenReference screen);
	void HoverUpdate(ScreenReference screen);
	void ExecuteAction(Coordinate mousePos);
	void ExecuteSecondaryAction(Coordinate mousePos);
}