namespace ConsoleIDE.Pages;

public interface IView
{
	void Update(ScreenReference screen);
	void InitFrozens();
}
