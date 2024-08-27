using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class DirectoryView(Coordinate pos, string dir, int widthBound, ScreenReference screen, Action<FileInfo> onFileSelect)
{
	readonly Coordinate pos = pos;
	readonly DirectoryInfo baseDir = new(dir);
	readonly List<string> openDirs = [];
	readonly List<string> openItemList = [];
	readonly ScreenReference screen = screen;
	readonly Action<FileInfo> fileSelect = onFileSelect;
	string selectedItem = dir;
	public int WidthBound = widthBound;

	public void Render()
	{
		openItemList.Clear();
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
		openItemList.Add(dir.FullName);

		if (dir.Name == ".git") return startPos.Y;

		if (selectedItem == dir.FullName)
		{
			ClickDelegator.Register(
				new RichTextButton(
					startPos,
					GetDisplayName(dir, startPos),
					(mousePos) => {
						if (openDirs.Contains(dir.FullName))
							openDirs.Remove(dir.FullName); // do we need to optimize?
						else
							openDirs.Add(dir.FullName);
					},
					CursesAttribute.REVERSE
				)
			);
		}
		else
		{
			ClickDelegator.Register(
				new PureTextButton(
					startPos,
					GetDisplayName(dir, startPos),
					(mousePos) => {
						if (!openDirs.Remove(dir.FullName))
							openDirs.Add(dir.FullName);
					}
				)
			);
		}

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
			openItemList.Add(file.FullName);

			string dispName = GetDisplayName(file, startPos);

			if (selectedItem == file.FullName)
			{
				ClickDelegator.Register(
					new RichTextButton(
						startPos,
						GetDisplayName(file, startPos),
						(mousePos) => fileSelect(file),
						CursesAttribute.REVERSE
					)
				);
			}
			else
			{
				ClickDelegator.Register(
					new PureTextButton(
						startPos,
						GetDisplayName(file, startPos),
						(mousePos) => fileSelect(file)
					)
				);
			}

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

	public void SendKey(int key)
	{
		if (key == CursesKey.UP)
		{
			int idx;

			if ((idx = openItemList.IndexOf(selectedItem)) == 0) return; // can't go up...

			selectedItem = openItemList[idx-1];

			return;
		}
		
		if (key == CursesKey.DOWN)
		{
			int idx;

			if ((idx = openItemList.IndexOf(selectedItem)) == openItemList.Count-1) return; // can't go down...

			selectedItem = openItemList[idx+1];

			return;
		}

		if (key == '\n')
		{
			if (selectedItem is null) return;

			if (Directory.Exists(selectedItem))
			{
				if (!openDirs.Remove(selectedItem))
					openDirs.Add(selectedItem);
			}
			else fileSelect(new FileInfo(selectedItem));

			return;
		}
	}

	public void SendMouseEvent(MouseEvent ev)
	{

	}
}