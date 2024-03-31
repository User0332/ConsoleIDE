using ConsoleIDE.Buttons;
using CursorPair = (ConsoleIDE.Pages.Project.EditingIndex controlling, ConsoleIDE.Pages.Project.EditingIndex notControlling);

namespace ConsoleIDE.Pages.Project;

#pragma warning disable CS0660, 0661
class EditingIndex(bool enabled)
{
	public readonly bool Enabled = enabled;
	public int X = 0;
	public int Y = 0;

	public static bool operator<(EditingIndex self, EditingIndex other)
	{
		return self.Y < other.Y || (self.X < other.X) && self.Y == other.Y;
	}

	public static bool operator>(EditingIndex self, EditingIndex other)
	{
		return self.Y > other.Y || (self.X > other.X) && self.Y == other.Y;
	}

	public bool Equals(EditingIndex other)
	{
		return (X == other.X) && (Y == other.Y);
	}

	public static bool operator==(EditingIndex self, EditingIndex other)
	{
		return self.Equals(other);
	}

	public static bool operator!=(EditingIndex self, EditingIndex other)
	{
		return !(self == other);
	}
}

#pragma warning restore CS0660, 0661

public class FileView(Coordinate pos, int widthBound)
{
	readonly Coordinate pos = pos;
	readonly List<(List<string> lines, CursorPair)> undos = [];
	readonly List<(List<string> lines, CursorPair)> redos = [];
	readonly List<CursorPair> cursors = [];
	FileInfo? currentFile;
	List<string> currLines = [];
	bool editing = false;
	bool saved = true;
	int secSinceLastChange = DateTime.Now.Second;
	public int WidthBound = widthBound;

	int CurrentLineRealMaxIdx
	{
		get => currLines[cursors[0].controlling.Y].Length;
	}

	int GetEditingDisplayXIndex(EditingIndex cursor)
	{
		string currLine = currLines[cursor.Y];

		int displayIndex = 0;

		for (int i = 0; i < cursor.X; i++)
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

		foreach (CursorPair cursorPair in cursors)
		{
			DisplayCursorPair(cursorPair);
		}
	}

	void DisplayCursorPair(CursorPair cursorPair)
	{
		if (!cursorPair.notControlling.Enabled)
		{
			Utils.MoveChangeAttr(
				cursorPair.controlling.Y+3,
				GetEditingDisplayXIndex(cursorPair.controlling)+pos.X,
				1,
				CursesAttribute.REVERSE
			);

			NCurses.SetCursor(0);

			return;
		}

		// find out which one is really at the beginning
		EditingIndex begin, end;

		if (cursorPair.controlling > cursorPair.notControlling)
		{
			begin = cursorPair.notControlling;
			end = cursorPair.controlling;
		}
		else
		{
			begin = cursorPair.controlling;
			end = cursorPair.notControlling;
		}

		// NCurses.Move(
		// 	begin.Y+3,
		// 	GetEditingDisplayXIndex(begin)+pos.X
		// );
		// NCurses.AttributeSet(CursesAttribute.REVERSE);




		// NCurses.Move(
		// 	end.Y+3,
		// 	GetEditingDisplayXIndex(end)+pos.X
		// );
		// NCurses.AttributeSet(CursesAttribute.REVERSE);

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
			cursors[0].controlling.Y = Math.Max(cursors[0].controlling.Y-1, 0);
			cursors[0].controlling.X = Math.Min(cursors[0].controlling.X, CurrentLineRealMaxIdx);
		
			return;
		}
		
		if (key == CursesKey.DOWN)
		{
			cursors[0].controlling.Y = Math.Min(cursors[0].controlling.Y+1, currLines.Count-1);
			cursors[0].controlling.X = Math.Min(cursors[0].controlling.X, CurrentLineRealMaxIdx);
			
			return;
		}

		if (key == CursesKey.LEFT)
		{
			if (cursors[0].controlling.X == 0)
			{
				if (cursors[0].controlling.Y == 0) return;

				cursors[0].controlling.Y-=1;
				cursors[0].controlling.X = CurrentLineRealMaxIdx;

				return;
			}

			cursors[0].controlling.X-=1;
			
			return;
		}

		if (key == CursesKey.RIGHT)
		{
			if (cursors[0].controlling.X == CurrentLineRealMaxIdx)
			{
				if (cursors[0].controlling.Y == currLines.Count-1) return;

				cursors[0].controlling.Y+=1;
				cursors[0].controlling.X = 0;

				return;
			}

			cursors[0].controlling.X+=1;
			
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
			if (cursors[0].controlling.X == 0)
			{
				if (cursors[0].controlling.Y == 0) return;
				
				SendKey(CursesKey.LEFT);

				currLines[cursors[0].controlling.Y] = currLines[cursors[0].controlling.Y]+currLines[cursors[0].controlling.Y+1];
				currLines.RemoveAt(cursors[0].controlling.Y+1);

				PushChange();

				return;
			}

			SendKey(CursesKey.LEFT);

			currLines[cursors[0].controlling.Y] = currLines[cursors[0].controlling.Y].Remove(cursors[0].controlling.X, 1);

			return;
		}

		if (key == '\n')
		{
			string slicedText = currLines[cursors[0].controlling.Y][cursors[0].controlling.X..];
			string leftText = currLines[cursors[0].controlling.Y][..cursors[0].controlling.X];

			currLines[cursors[0].controlling.Y] = leftText;

			currLines.Insert(cursors[0].controlling.Y+1, slicedText);
			
			cursors[0].controlling.Y+=1;
			cursors[0].controlling.X = 0;

			PushChange();

			return;
		}

		currLines[cursors[0].controlling.Y] = currLines[cursors[0].controlling.Y].Insert(cursors[0].controlling.X, char.ConvertFromUtf32(key));

		if ((DateTime.Now.Second - secSinceLastChange) >= 30) PushChange();

		SendKey(CursesKey.RIGHT);
	}

	public void PushChange()
	{
		if (!undos[^1].lines.SequenceEqual(currLines)) // has a change been made?
		{
			undos.Add(([..currLines], cursors[0]));

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

		redos.Add(([..currLines], cursors[0]));

		cursors.Clear();
		cursors.Add(new());

		(currLines, cursors[0]) = undos[^1];

		if (undos.Count == 1) return;

		undos.RemoveAt(undos.Count-1);
	}

	public void TryRedo()
	{
		if (redos.Count == 0) return;

		undos.Add(([..currLines], cursors[0]));
		
		cursors.Clear();
		cursors.Add(new());

		(currLines, cursors[0]) = redos[^1];

		redos.RemoveAt(redos.Count-1);		
	}

	public void ToggleEditingMode()
	{
		editing = !editing;
	}

	public void ChangeTo(FileInfo file)
	{
		currentFile = file;
		currLines = [.. File.ReadAllLines(file.FullName)];

		cursors.Clear();
		cursors.Add((new EditingIndex(true), new(false)));

		undos.Clear();
		undos.Add(([..currLines], cursors[0]));
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