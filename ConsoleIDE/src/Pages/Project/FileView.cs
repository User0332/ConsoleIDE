using ConsoleIDE.Buttons;

namespace ConsoleIDE.Pages.Project;

public class FileView(Coordinate pos, int widthBound)
{
	readonly Coordinate pos = pos;
	readonly List<(List<string> lines, int editingYIndex, int editingXIndex)> undos = [];
	readonly List<(List<string> lines, int editingYIndex, int editingXIndex)> redos = [];
	FileInfo? currentFile;
	List<string> currLines = [];
	bool editing = false;
	bool saved = true;
	int editingYIndex;
	int editingXIndex;
	int secSinceLastChange = DateTime.Now.Second;
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

			PushChange();
			
			File.WriteAllLines(currentFile.FullName, [.. currLines]);
			return;
		}

		if (!editing) return;

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

		saved = false; // anything past this will edit the file

		if (key == Utils.CTRL('z'))
		{
			TryPopChange();
			return;
		}

		if (key == Utils.CTRL('y'))
		{
			TryRedo();
			return;
		}

		if (redos.Count != 0) redos.Clear();

		if (key == CursesKey.BACKSPACE)
		{
			if (editingXIndex == 0)
			{
				if (editingYIndex == 0) return;
				
				SendKey(CursesKey.LEFT);

				currLines[editingYIndex] = currLines[editingYIndex]+currLines[editingYIndex+1];
				currLines.RemoveAt(editingYIndex+1);

				PushChange();

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

			PushChange();

			return;
		}

		currLines[editingYIndex] = currLines[editingYIndex].Insert(editingXIndex, char.ConvertFromUtf32(key));

		if ((DateTime.Now.Second - secSinceLastChange) >= 30) PushChange();

		SendKey(CursesKey.RIGHT);
	}

	public void PushChange()
	{
		if (!undos[^1].lines.SequenceEqual(currLines)) // has a change been made?
		{
			undos.Add(([..currLines], editingXIndex, editingYIndex));

			if (undos.Count > 30)
			{
				undos.RemoveAt(0);
			}
		}

		secSinceLastChange = DateTime.Now.Second;
	}

	public void TryPopChange()
	{
		if (undos.Count == 0) return;

		redos.Add(([..currLines], editingXIndex, editingYIndex));

		(currLines, editingXIndex, editingYIndex) = undos[^1];

		if (undos.Count == 1) return;

		undos.RemoveAt(undos.Count-1);
	}

	public void TryRedo()
	{
		if (redos.Count == 0) return;

		undos.Add(([..currLines], editingXIndex, editingYIndex));
		
		(currLines, editingXIndex, editingYIndex) = redos[^1];

		redos.RemoveAt(redos.Count-1);		
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

		undos.Clear();
		undos.Add(([..currLines], editingXIndex, editingYIndex));
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