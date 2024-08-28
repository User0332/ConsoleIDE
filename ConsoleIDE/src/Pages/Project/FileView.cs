using ConsoleIDE.AnalyzerWrappers;
using ConsoleIDE.Buttons;
using ConsoleIDE.ThemeWrapper;


// A CursorPair is a tuple that either represents a cursor or a selection of text
// the "controlling" cursor is the one that can be moved via the arrow keys, while the nonControlling (if enabled) just delimits the text selection
using CursorPair = (ConsoleIDE.Pages.Project.EditingIndex controlling, ConsoleIDE.Pages.Project.EditingIndex notControlling);

namespace ConsoleIDE.Pages.Project;

#pragma warning disable CS0660, CS0661
class EditingIndex(bool enabled) // TODO: figure out why this can't be a struct
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

#pragma warning restore CS0660, CS0661

public class FileView(Coordinate pos, int widthBound, string projectDir)
{
	readonly Coordinate viewPos = pos;
	readonly List<(List<string> lines, CursorPair)> undos = [];
	readonly List<(List<string> lines, CursorPair)> redos = [];
	readonly List<CursorPair> cursors = [];
	readonly Theme currentTheme = ThemeLoader.LoadThemeFromProjectPathOrDefault(projectDir);
	readonly SourceFileAnalyzer? sourceFileAnalyzer = File.Exists($"{projectDir}/{new DirectoryInfo(projectDir).Name}.csproj") ? new($"{projectDir}/{new DirectoryInfo(projectDir).Name}.csproj") : null;
	FileInfo? currentFile;
	List<string> currLines = [];
	bool editing = false;
	bool saved = true;
	int secSinceLastChange = DateTime.Now.Second;
	int yScroll;
	public int WidthBound = widthBound;

	int CurrentLineRealMaxIdx
	{
		get => currLines[cursors[0].controlling.Y].Length;
	}

	int LongestLineNoLength
	{
		get => currLines.Count.ToString().Length;
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

		return displayIndex+1; // TODO: figure out why this +1 is needed
	}

	public void Render()
	{
		if (currentFile is null)
		{
			AddStr(viewPos, "No File Selected!");
			return;
		}

		string editingMessage = editing ? "[editing] " : "[viewing] ";
		editingMessage+=saved ? "[saved]" : "[unsaved]";

		// ExitButton.Size.X+1 (+1 for padding)
		// -4 for two spaces and two parentheses (see below AddStr call)
		int maxFileNameLen = WidthBound-viewPos.X-editingMessage.Length-currentFile.Name.Length-(ExitButton.Size.X+1)-4;

		string fullDisplayFileName = currentFile.FullName;

		if (fullDisplayFileName.Length > maxFileNameLen)
		{
			fullDisplayFileName = fullDisplayFileName[..(maxFileNameLen-3)]+"...";
		}

		AddStr(viewPos, $"{currentFile.Name} ({fullDisplayFileName}) {editingMessage}");
		AddStr(viewPos.AddY(1), new string('_', WidthBound-viewPos.X));

		if (!editing) currLines = [.. File.ReadAllText(currentFile.FullName).Split(Environment.NewLine)]; // if we're just viewing, update the file contents on each re-render

		DisplayFileContents(viewPos.AddY(3));

		if (editing)
		{
			foreach (CursorPair cursorPair in cursors)
			{
				DisplayCursorPair(cursorPair);
			}

			NCurses.SetCursor(0);
		}
	}

	void DisplayCursorPair(CursorPair cursorPair)
	{
		if (!cursorPair.notControlling.Enabled)
		{
			Utils.MoveChangeAttr(
				cursorPair.controlling.Y+3,
				GetEditingDisplayXIndex(cursorPair.controlling)+viewPos.X+LongestLineNoLength,
				1,
				CursesAttribute.REVERSE
			);

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

		for (int i = begin.Y; i < end.Y; i++)
		{
			Utils.MoveChangeAttr(
				i+3,
				0+viewPos.X+LongestLineNoLength,
				currLines[i].Length,
				CursesAttribute.REVERSE
			);
		}


		Utils.MoveChangeAttr(
			end.Y+3,
			GetEditingDisplayXIndex(end)+viewPos.X+LongestLineNoLength,
			currLines[end.Y][..end.X].Length,
			CursesAttribute.REVERSE
		);

		Utils.MoveChangeAttr(
			begin.Y+3,
			GetEditingDisplayXIndex(begin)+viewPos.X+LongestLineNoLength,
			currLines[begin.Y][begin.X..].Length,
			CursesAttribute.REVERSE
		);
	}

	void DisplayFileContents(Coordinate pos)
	{
		if ((sourceFileAnalyzer is null) || !currentFile!.Name.EndsWith(".cs"))
		{
			for (int i = yScroll; i < currLines.Count; i++)
			{
				string lineNo = (i+1).ToString();

				NCurses.AttributeOn(Utils.COLOR_PAIR(1));
				AddStr(pos, lineNo);
				NCurses.AttributeOff(Utils.COLOR_PAIR(1));
				// Utils.MoveChangeAttr(pos.Y, pos.X, lineNo.Length, CursesAttribute.NORMAL, 1);

				AddStr(pos.AddX(LongestLineNoLength+1), currLines[i].Replace("\t", "    "));
				
				pos = pos.AddY(1);
			}
			
			return;
		}

		var annotatedLines = sourceFileAnalyzer.GetAnalyzedLinesAsSourceSegments(
			currentFile.FullName, currLines
		);

		for (int i = yScroll; i < annotatedLines.Count; i++)
		{
			string lineNo = (i+1).ToString();

			NCurses.AttributeOn(Utils.COLOR_PAIR(1));
			AddStr(pos, lineNo);
			NCurses.AttributeOff(Utils.COLOR_PAIR(1));
			// Utils.MoveChangeAttr(pos.Y, pos.X, lineNo.Length, CursesAttribute.NORMAL, 1);

			int currX = 0;
			int j = 0;

			while (j < annotatedLines[i].Count)
			{
				var segment = annotatedLines[i][j];

				var coloredAttr = Utils.COLOR_PAIR(Theme.GetColorPairNumber(segment.Type));

				var displayText = segment.Text.Replace("\t", "    ");

				NCurses.AttributeOn(coloredAttr);
				AddStr(pos.AddX(currX), displayText);
				NCurses.AttributeOff(coloredAttr);

				currX+=displayText.Length;
				j++;
			}
			
			pos = pos.AddY(1);
		}
	}

	public void SendMouseEvent(MouseEvent ev)
	{
		if (currentFile is null) return;

		if (Utils.IsMouseEventType(ev, Utils.MOUSE_SCROLL_UP))
		{
			yScroll = Math.Max(0, yScroll-1);

			return;
		}
		else if (Utils.IsMouseEventType(ev, Utils.MOUSE_SCROLL_DOWN))
		{
			yScroll = Math.Min(Math.Max(0, currLines.Count-(Utils.GetWindowHeight(GlobalScreen.Screen)-3)), yScroll+1);
		
			return;
		}
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

			// TODO: autoscroll
		
			return;
		}
		
		if (key == CursesKey.DOWN)
		{
			cursors[0].controlling.Y = Math.Min(cursors[0].controlling.Y+1, currLines.Count-1);
			cursors[0].controlling.X = Math.Min(cursors[0].controlling.X, CurrentLineRealMaxIdx);

			// TODO: autoscroll
			
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

		saved = false; // anything past this will edit the file

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

		if ((DateTime.Now.Second - secSinceLastChange) >= 20) PushChange();

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

		saved = false;

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

		saved = false;

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
		currLines = [.. File.ReadAllText(file.FullName).Split(Environment.NewLine)];

		yScroll = 0;

		cursors.Clear();
		cursors.Add((new EditingIndex(true), new(false)));

		undos.Clear();
		undos.Add(([..currLines], cursors[0]));
	}

	void AddStr(Coordinate pos, string message)
	{
		int widthLeft = WidthBound-pos.X-1;

		if (widthLeft < 0) return;

		if (message.Length > widthLeft)
		{
			message = message[..widthLeft];
		}

		Utils.AddStr(pos, message);
	}
}