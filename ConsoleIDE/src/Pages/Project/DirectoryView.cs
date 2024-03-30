using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class DirectoryView(Coordinate pos, string dir, int maxWidth, ScreenReference screen, Action<FileInfo> onFileSelect)
{
	readonly Coordinate pos = pos;
	readonly DirectoryInfo baseDir = new(dir);
	readonly List<string> openDirs = [];
	readonly int maxWidth = maxWidth;
	readonly ScreenReference screen = screen;
	readonly Action<FileInfo> fileSelect = onFileSelect;

	public void Render()
	{
		RecurseDir(baseDir, pos);
	}

	int RecurseDir(DirectoryInfo dir, Coordinate startPos)
	{
		if (dir.Name == ".git") return startPos.Y;

		ClickDelegator.Register(
			new PureTextButton(
				startPos,
				GetDisplayName(dir),
				(mousePos) => {
					if (openDirs.Contains(dir.FullName))
						openDirs.Remove(dir.FullName); // do we need to optimize?
					else
						openDirs.Add(dir.FullName);
				}
			)
		);

		startPos = startPos.AddTo(new(2, 1)); // indent on x axis and inc on y axis for use of one line above

		if (!openDirs.Contains(dir.FullName)) return startPos.Y;

		foreach (var subDir in dir.GetDirectories())
		{
			startPos = startPos.WithY( // update current tracking y val
				RecurseDir(subDir, startPos)
			);
		}

		foreach (var file in dir.GetFiles())
		{
			ClickDelegator.Register(
				new PureTextButton(
					startPos,
					GetDisplayName(file),
					(mousePos) => fileSelect(file)
					
				)
			);

			startPos = startPos.AddY(1); // inc one y per line
		}

		return startPos.Y;
	}


	string GetDisplayName(DirectoryInfo dir)
	{
		if (openDirs.Contains(dir.FullName))
			return $"v {dir.Name}/";
	
		return $"> {dir.Name}/";
	}

	static string GetDisplayName(FileInfo file)
	{
		return file.Name;
	}
}