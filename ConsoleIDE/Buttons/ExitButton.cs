using System.Runtime.InteropServices;

namespace ConsoleIDE.Buttons;

public class ExitButton(Coordinate pos) : IButton
{
	readonly Coordinate startPos = pos;
	readonly ClickableBound bound = new(pos, pos.AddTo(Size));

	public static readonly Coordinate Size = new(3, 1);
	public ClickableBound BoundingBox => bound;

	public ExitButton() : this(new(Utils.GetWindowWidth(GlobalScreen.Screen)-Size.X, 0)) {}

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
		NativeLibrary.Free(Utils.CursesLib);
		Environment.Exit(0);
	}

	public void ExecuteSecondaryAction(Coordinate mousePos)
	{
		
	}
}