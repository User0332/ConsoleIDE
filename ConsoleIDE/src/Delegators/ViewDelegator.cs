using ConsoleIDE.Pages;

namespace ConsoleIDE.Delegators;

public static class ViewDelegator
{
	#pragma warning disable CS8618
	
	static IView CurrentView;

	#pragma warning restore CS8618

	static readonly Stack<IView> PrevViews = new();

	public static void Init(ScreenReference screen)
	{
		Push(new DefaultView(screen));
	}

	public static void Push(IView newView)
	{
		NCurses.Clear();
		ClickDelegator.Clear();
		ClickDelegator.ClearFrozen();

		PrevViews.Push(CurrentView);

		CurrentView = newView;

		CurrentView.InitFrozens();
	}

	public static void Pop()
	{
		NCurses.Clear();
		ClickDelegator.Clear();
		ClickDelegator.ClearFrozen();
		
		CurrentView = PrevViews.Pop();

		CurrentView.InitFrozens();
	}

	public static void Render(ScreenReference screen)
	{
		CurrentView.Update(screen);
	}
}