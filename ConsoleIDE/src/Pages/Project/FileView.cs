using System.Security.Cryptography.X509Certificates;
using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class FileView(Coordinate pos, int widthBound)
{
	readonly Coordinate pos = pos;
	FileInfo? currentFile;
	List<string> currLines = [];
	bool editing = false;
	bool saved = true;
	int editingYIndex;
	int editingXIndex;
	public int WidthBound = widthBound;

	int CurrentLineRealMaxIdx
	{
		get => currLines[editingYIndex].Length;
	}

	int EditingDisplayXIndex
	{
		get {
			string currLine = currLines[editingYIndex];

			int displayIndex = 0;

			for (int i = 0; i < editingXIndex; i++)
			{
				if (currLine[i] == '\t')
				{
					displayIndex+=4; // tab_size=4
					continue;
				}

				displayIndex+=1;
			}

			return displayIndex;
		}
	}

	public void Render()
	{
		if (currentFile is null)
		{
			AddStr(pos, "No File Selected!");
			return;
		}

		string editingMessage = editing ? "[editing] " : "[viewing] ";
		editingMessage+=saved ? "[saved]" : "[unsaved]";

		AddStr(pos, $"{currentFile.Name} ({currentFile.FullName}) {editingMessage}");
		AddStr(pos.AddY(1), new string('_', WidthBound-pos.X));

		DisplayFileContents(pos.AddY(3));

		NCurses.Move(
			editingYIndex+3,
			EditingDisplayXIndex+pos.X
		);
	}

	List<string> DisplayFileContents(Coordinate pos)
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

		if (key == Utils.CTRL('e'))
		{
			ToggleEditingMode();
			
			return;
		}

		if (key == Utils.CTRL('s'))
		{
			saved = true;
			File.WriteAllLines(currentFile.FullName, [.. currLines]);
			return;
		}

		if (key == CursesKey.UP)
		{
			editingYIndex = Math.Max(editingYIndex-1, 0);
			editingXIndex = Math.Min(editingXIndex, CurrentLineRealMaxIdx);
		
			return;
		}
		
		if (key == CursesKey.DOWN)
		{
			editingYIndex = Math.Min(editingYIndex+1, currLines.Count-1);
			editingXIndex = Math.Min(editingXIndex, CurrentLineRealMaxIdx);
			
			return;
		}

		if (key == CursesKey.LEFT)
		{
			if (editingXIndex == 0)
			{
				if (editingYIndex == 0) return;

				editingYIndex-=1;
				editingXIndex = CurrentLineRealMaxIdx;

				return;
			}

			editingXIndex-=1;
			
			return;
		}

		if (key == CursesKey.RIGHT)
		{
			if (editingXIndex == CurrentLineRealMaxIdx)
			{
				if (editingYIndex == currLines.Count-1) return;

				editingYIndex+=1;
				editingXIndex = 0;

				return;
			}

			editingXIndex+=1;
			
			return;
		}
		
		if (!editing) return;

		saved = false;

		if (key == CursesKey.BACKSPACE)
		{
			if (editingXIndex == 0)
			{
				if (editingYIndex == 0) return;
				
				SendKey(CursesKey.LEFT);

				currLines[editingYIndex] = currLines[editingYIndex]+currLines[editingYIndex+1];
				currLines.RemoveAt(editingYIndex+1);

				return;
			}

			SendKey(CursesKey.LEFT);

			currLines[editingYIndex] = currLines[editingYIndex].Remove(editingXIndex, 1);

			return;
		}

		if (key == '\n')
		{
			string slicedText = currLines[editingYIndex][editingXIndex..];
			string leftText = currLines[editingYIndex][..editingXIndex];

			currLines[editingYIndex] = leftText;

			currLines.Insert(editingYIndex+1, slicedText);
			
			editingYIndex+=1;
			editingXIndex = 0;

			return;
		}

		currLines[editingYIndex] = currLines[editingYIndex].Insert(editingXIndex, char.ConvertFromUtf32(key));

		SendKey(CursesKey.RIGHT);
	}

	public void ToggleEditingMode()
	{
		editing = !editing;

		NCurses.SetCursor(editing ? 1 : 0);
	}

	public void ChangeTo(FileInfo file)
	{
		currentFile = file;
		currLines = [.. File.ReadAllLines(file.FullName)];
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