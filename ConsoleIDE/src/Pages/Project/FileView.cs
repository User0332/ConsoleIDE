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
	public int HeightBound = Utils.GetWindowHeight(GlobalScreen.Screen)-3;

	int CurrentLineVisualMaxIdx
	{
		get => currLines[cursors[0].controlling.Y+yScroll].Length;
	}

	int LongestLineNoLength
	{
		get => currLines.Count.ToString().Length;
	}

	int GetEditingDisplayXIndex(EditingIndex cursor)
	{
		string currLine = currLines[cursor.Y+yScroll];

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

		if (!editing) LoadFileIntoCurrLines(); // if we're just viewing, update the file contents on each re-render

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

		for (int i = begin.Y+yScroll; i < end.Y+yScroll; i++)
		{
			Utils.MoveChangeAttr(
				i+3,
				0+viewPos.X+LongestLineNoLength,
				currLines[i+yScroll].Length,
				CursesAttribute.REVERSE
			);
		}


		Utils.MoveChangeAttr(
			end.Y+3,
			GetEditingDisplayXIndex(end)+viewPos.X+LongestLineNoLength,
			currLines[end.Y+yScroll][..end.X].Length,
			CursesAttribute.REVERSE
		);

		Utils.MoveChangeAttr(
			begin.Y+3,
			GetEditingDisplayXIndex(begin)+viewPos.X+LongestLineNoLength,
			currLines[begin.Y+yScroll][begin.X..].Length,
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

		for (int i = yScroll; i < annotatedLines.Length; i++)
		{
			string lineNo = (i+1).ToString();

			NCurses.AttributeOn(Utils.COLOR_PAIR(1));
			AddStr(pos, lineNo);
			NCurses.AttributeOff(Utils.COLOR_PAIR(1));
			// Utils.MoveChangeAttr(pos.Y, pos.X, lineNo.Length, CursesAttribute.NORMAL, 1);

			int currX = LongestLineNoLength+1;
			int j = 0;

			while (j < annotatedLines[i].Length)
			{
				var segment = annotatedLines[i][j];

				var coloredAttr = Utils.COLOR_PAIR(segment.ColorPairNumber);

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
			if (cursors[0].controlling.Y == 0) // we are at the top of the screen and must use yScroll to move the cursor up
			{
				if (yScroll == 0) return; // we are at the very top of the screen & the file, we can't move further up

				yScroll--;

			}
			else cursors[0].controlling.Y--; // otherwise, we can just move the cursor up visually

			cursors[0].controlling.X = Math.Min(cursors[0].controlling.X, CurrentLineVisualMaxIdx);


			return;
		}
		
		if (key == CursesKey.DOWN)
		{
			// autoscroll if down arrow pressed & we are at the bottom of the screen and not at the end of the file

			bool endOfFileVisible = currLines.Count-yScroll <= HeightBound;

			if ((cursors[0].controlling.Y+1) == HeightBound) // we are at the bottom of the screen, we must autoscroll
			{
				if (endOfFileVisible) return; // we can't go further down

				yScroll++;
			}
			else if ((cursors[0].controlling.Y+yScroll) != currLines.Count-1) cursors[0].controlling.Y++; // otherwise, we can freely move down if we are not on the last line
			
			cursors[0].controlling.X = Math.Min(cursors[0].controlling.X, CurrentLineVisualMaxIdx);

			return;
		}

		if (key == CursesKey.LEFT)
		{
			if (cursors[0].controlling.X == 0)
			{
				if (cursors[0].controlling.Y == 0) // cursor is at top of screen, try autoscrolling
				{
					if (yScroll == 0) return;  // at first line in file, cannot move more left

					yScroll--; // use yScroll to decrement cursor line index
				}
				else cursors[0].controlling.Y--; // else move cursor visually

				cursors[0].controlling.X = CurrentLineVisualMaxIdx;

				return;
			}

			cursors[0].controlling.X--;
			
			return;
		}

		if (key == CursesKey.RIGHT)
		{
			if (cursors[0].controlling.X == CurrentLineVisualMaxIdx)
			{
				if (cursors[0].controlling.Y+yScroll == currLines.Count-1) return; // last line in file, cannot move further

				if ((cursors[0].controlling.Y+1) == HeightBound) yScroll++; // autoscroll if at bottom of screen
				else cursors[0].controlling.Y++; // else move cursor visually
				
				cursors[0].controlling.X = 0;

				return;
			}

			cursors[0].controlling.X++;
			
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

		int realCursorYIndex = cursors[0].controlling.Y+yScroll;
		int realCursorXIndex = cursors[0].controlling.X; // TODO: add xscroll

		if (key == CursesKey.BACKSPACE)
		{
			if (realCursorXIndex == 0)
			{
				if (realCursorYIndex == 0) return;
				
				bool endOfFileVisible = currLines.Count-yScroll <= HeightBound;

				SendKey(CursesKey.LEFT);

				// SendKey(LEFT) changes cursor position, so we need to update these values
				realCursorYIndex = cursors[0].controlling.Y+yScroll;

				currLines[realCursorYIndex] = currLines[realCursorYIndex]+currLines[realCursorYIndex+1];
				currLines.RemoveAt(realCursorYIndex+1);

				PushChange();

				if (endOfFileVisible && yScroll > 0) yScroll--;

				return;
			}

			SendKey(CursesKey.LEFT);

			// SendKey(LEFT) changes cursor position, so we need to update these values
			realCursorYIndex = cursors[0].controlling.Y+yScroll;
			realCursorXIndex = cursors[0].controlling.X;

			currLines[realCursorYIndex] = currLines[realCursorYIndex].Remove(realCursorXIndex, 1);

			return;
		}

		if (key == '\n')
		{
			string slicedText = currLines[realCursorYIndex][realCursorXIndex..];
			string leftText = currLines[realCursorYIndex][..realCursorXIndex];

			currLines[realCursorYIndex] = leftText;

			currLines.Insert(realCursorYIndex+1, slicedText);
			
			SendKey(CursesKey.DOWN);
			cursors[0].controlling.X = 0;

			PushChange();

			return;
		}

		currLines[realCursorYIndex] = currLines[realCursorYIndex].Insert(cursors[0].controlling.X, char.ConvertFromUtf32(key));

		if ((DateTime.Now.Second - secSinceLastChange) >= 20) PushChange();

		SendKey(CursesKey.RIGHT);
	}

	void LoadFileIntoCurrLines()
	{
		var lines = File.ReadAllLines(currentFile!.FullName);

		if (lines.Length == 0)
		{
			currLines = [""];
			return;
		}

		currLines = [..lines];
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
		LoadFileIntoCurrLines();

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