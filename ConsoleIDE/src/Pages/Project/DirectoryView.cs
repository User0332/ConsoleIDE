using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class DirectoryView(Coordinate pos, string dir, int widthBound, ScreenReference screen, Action<FileInfo> onFileSelect)
{
	readonly Coordinate pos = pos;
	readonly DirectoryInfo baseDir = new(dir);
	readonly List<string> openDirs = [];
	readonly ScreenReference screen = screen;
	readonly Action<FileInfo> fileSelect = onFileSelect;
	public int WidthBound = widthBound;

	public void Render()
	{
		RecurseDir(baseDir, pos);
		DrawBar();
	}

	void DrawBar()
	{
		int height = Utils.GetWindowHeight(screen);

		for (int i = 0; i < height; i++)
		{
			NCurses.MoveAddChar(i, WidthBound, '|');
		}
	}

	int RecurseDir(DirectoryInfo dir, Coordinate startPos)
	{
		if (dir.Name == ".git") return startPos.Y;

		ClickDelegator.Register(
			new PureTextButton(
				startPos,
				GetDisplayName(dir, startPos),
				(mousePos) => {
					if (openDirs.Contains(dir.FullName))
						openDirs.Remove(dir.FullName); // do we need to optimize?
					else
						openDirs.Add(dir.FullName);
				}
			)
		);

		startPos = startPos.AddTo(new(1, 1)); // indent on x axis and inc on y axis for use of one line above

		if (!openDirs.Contains(dir.FullName)) return startPos.Y;

		foreach (var subDir in dir.GetDirectories())
		{
			startPos = startPos.WithY( // update current tracking y val
				RecurseDir(subDir, startPos)
			);
		}

		foreach (var file in dir.GetFiles())
		{
			string dispName = GetDisplayName(file, startPos);

			ClickDelegator.Register(
				new PureTextButton(
					startPos,
					GetDisplayName(file, startPos),
					(mousePos) => fileSelect(file)
					
				)
			);

			startPos = startPos.AddY(1); // inc one y per line
		}

		return startPos.Y;
	}


	string GetDisplayName(DirectoryInfo dir, Coordinate pos)
	{
		int maxWidth = WidthBound-pos.X-1;

		if (maxWidth <= 0) return "";

		string dispName = openDirs.Contains(dir.FullName) ? $"v {dir.Name}/" : $"> {dir.Name}/";

		if (dispName.Length <= maxWidth) return dispName;

		dispName = dispName[..Math.Max(0, maxWidth-4)]+".../";

		return dispName;
	}

	string GetDisplayName(FileInfo file, Coordinate pos)
	{
		int maxWidth = WidthBound-pos.X-1;

		if (maxWidth <= 0) return "";

		string dispName = file.Name;

		if (dispName.Length <= maxWidth) return dispName;

		dispName = dispName[..Math.Max(0, maxWidth-3)]+"...";
		
		return dispName;
	}
}