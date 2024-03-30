using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class FileView(Coordinate pos, int widthBound)
{
	readonly Coordinate pos = pos;
	FileInfo? currentFile;
	string[] currLines = Array.Empty<string>();
	bool editing = false;
	int editingYIndex;
	int editingXIndex;
	public int WidthBound = widthBound;

	public void Render()
	{
		if (currentFile is null)
		{
			AddStr(pos, "No File Selected!");
			return;
		}

		string editingMessage = editing ? "[editing]" : "[viewing]";

		AddStr(pos, $"{currentFile.Name} ({currentFile.FullName}) {editingMessage}");
		AddStr(pos.AddY(1), new string('_', WidthBound-pos.X));

		DisplayFileContents(pos.AddY(3));

		NCurses.Move(
			editingYIndex+3,
			editingXIndex+pos.X
			);
	}

	string[] DisplayFileContents(Coordinate pos)
	{
		foreach (var line in currLines)
		{
			AddStr(pos, line.Replace("\t", "    "));
			
			pos = pos.AddY(1);
		}

		return currLines;
	}

	public void SendKey(int key)
	{
		if (currentFile is null) return;

		if (key == CursesKey.UP)
		{
			editingYIndex = Math.Max(editingYIndex-1, 0);
			editingXIndex = Math.Min(editingXIndex, CurrentLineMaxIdx);
		
			return;
		}
		
		if (key == CursesKey.DOWN)
		{
			editingYIndex = Math.Min(editingYIndex+1, currLines.Length-1);
			editingXIndex = Math.Min(editingXIndex, CurrentLineMaxIdx);
			
			return;
		}

		if (key == CursesKey.LEFT)
		{
			if (editingXIndex == 0)
			{
				if (editingYIndex == 0) return;

				editingYIndex-=1;
				editingXIndex =CurrentLineMaxIdx;

				return;
			}

			editingXIndex-=1;
			
			return;
		}

		if (key == CursesKey.RIGHT)
		{
			if (editingXIndex == CurrentLineMaxIdx)
			{
				if (editingYIndex == currLines.Length-1) return;

				editingYIndex+=1;
				editingXIndex = 0;

				return;
			}

			editingXIndex+=1;
			
			return;
		}
		
		if (!editing) return;

		currLines[editingYIndex] = currLines[editingYIndex].Insert(editingXIndex-(4*currLines[editingYIndex].Count(ch => ch == '\t')), char.ConvertFromUtf32(key)); // xidx-(3*currLines[editingYIndex].Count(ch => ch == '\t')) because we made the index count tabs as 4 chars (3 chars more)

	}

	int CurrentLineMaxIdx
	{
		get => Math.Max(0, currLines[editingYIndex].Replace("\t", "    ").Length-1);
	}

	public void ToggleEditingMode()
	{
		editing = !editing;
	}

	public void ChangeTo(FileInfo file)
	{
		currentFile = file;
		currLines = File.ReadAllLines(file.FullName);
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