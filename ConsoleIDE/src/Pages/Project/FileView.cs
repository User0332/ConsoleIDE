using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class FileView(Coordinate pos, int widthBound)
{
	readonly Coordinate pos = pos;
	FileInfo? currentFile;
	public int WidthBound = widthBound;

	public void Render()
	{
		if (currentFile is null)
		{
			AddStr(pos, "No File Selected!");
			return;
		}

		AddStr(pos, $"{currentFile.Name} ({currentFile.FullName})");
		AddStr(pos.AddY(1), new string('_', WidthBound-pos.X));

		DisplayFileContents(pos.AddY(3), currentFile.FullName);
	}

	void DisplayFileContents(Coordinate pos, string file)
	{
		string[] lines = File.ReadAllText(file).Split('\n');

		foreach (var line in lines)
		{
			AddStr(pos, line);
			
			pos = pos.AddY(1);
		}
	}

	public void ChangeTo(FileInfo file)
	{
		currentFile = file;
	}

	void AddStr(Coordinate pos, string message)
	{
		int widthLeft = WidthBound-pos.X-1;

		if (message.Length > widthLeft)
		{
			message = message[..widthLeft];
		}

		Utils.AddStr(pos, message);
	}
}