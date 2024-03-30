using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class FileView(Coordinate pos, int maxWidth)
{
	readonly Coordinate pos = pos;
	readonly int maxWidth = maxWidth;
	FileInfo? currentFile;

	public void Render()
	{
		if (currentFile is null)
		{
			Utils.AddStr(pos, "No File Selected!");
			return;
		}

		Utils.AddStr(pos, currentFile.Name);
	}

	public void ChangeTo(FileInfo file)
	{
		currentFile = file;
	}
}