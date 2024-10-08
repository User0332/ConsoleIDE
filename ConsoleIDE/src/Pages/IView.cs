namespace ConsoleIDE.Pages;

public interface IView
{
	void Update(ScreenReference screen);
	void InitFrozens();

	void RecieveKey(int key);
	void RecieveMouseInput(MouseEvent ev);
}
