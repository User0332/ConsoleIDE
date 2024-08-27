using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class ProjectView : IView
{
	readonly ScreenReference screen;
	readonly FileView fileEditor;
	readonly DirectoryView fileExplorer;
	bool editorSelected = false;

	public ProjectView(ScreenReference screen, string projectDir)
	{
		this.screen = screen;
		fileEditor = new(new(30, 0), Utils.GetWindowWidth(screen), projectDir);
		fileExplorer = new(new(0, 0), projectDir, 28, screen, fileEditor.ChangeTo);
	}

	public void InitFrozens()
	{
		NCurses.SetCursor(0);
		ClickDelegator.RegisterFrozen(new ExitButton());
		ClickDelegator.RegisterFrozen(new BackButton());
	}
	
	public void Update(ScreenReference screen)
	{
		ClickDelegator.ClearKeepScreen();

		fileExplorer.Render();
		fileEditor.Render();

	}

	public void RecieveKey(int key)
	{
		if (key == CursesKey.ESC)
		{
			var second = NCurses.GetChar();

			if (second == 'f') // ALT-F, toggle file explorer/file editor
			{
				editorSelected = !editorSelected;
				return;
			}
			else if (second == -1) // no second key pressed, just ESC was pressed instead of alt
			{
				// on ESC, we go back to the file explorer
				editorSelected = false;
				return;
			}

			// we don't want to consume this sequence, ungetch it and let flow continue so another handler can consume it

			NCurses.UngetChar(second);
		}

		if (key == Utils.CTRL('q'))
		{
			new ExitButton().ExecuteAction(new());
		}

		if (key == Utils.CTRL('t')) // TODO: change this to alt f or something more intuitive
		{
			editorSelected = !editorSelected;
			return;
		}

		if (editorSelected) fileEditor.SendKey(key); // TODO: be able to swap selection so you can navigate file explorer using keyboard
		else fileExplorer.SendKey(key);
	}

	public void RecieveMouseInput(MouseEvent ev)
	{
		if (editorSelected) fileEditor.SendMouseEvent(ev);
		else fileExplorer.SendMouseEvent(ev);
	}
}