using ConsoleIDE.AnalyzerWrappers;
using ConsoleIDE.Buttons;
using ConsoleIDE.ThemeWrapper;

namespace ConsoleIDE.Pages.Project;

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
	public int YScroll;
	public int XScroll;
	public int MaxYScroll => NumberOfLines-HeightBound; // TODO: fix, this isn't actually heightbound (its same semantics as widthbound vs maxcontentdisplaylength)
	public int MaxXScroll => LongestLineLength-MaxContentDisplayLength;
	public readonly int WidthBound = widthBound;
	public readonly int HeightBound = Utils.GetWindowHeight(GlobalScreen.Screen)-3; // todo: paramaterize
	public int CurrentLineLength => currLines[cursors[0].controlling.RealYIndex].Length;
	int LongestLineNoLength => NumberOfLines.ToString().Length;
	int LinePrefixLength => LongestLineNoLength+1; // longest line no + one space for extra padding
	int LongestLineLength => currLines.Max(line => line.Length);
	readonly int FilePrefixHeight = 3;
	public int NumberOfLines => currLines.Count;
	public int MaxContentDisplayLength => WidthBound-LinePrefixLength-viewPos.X-1;
	public int MaxContentDisplayHeight => HeightBound-FilePrefixHeight-viewPos.Y;

	int GetEditingDisplayXIndex(EditingIndex cursor)
	{
		string currLine = currLines[cursor.RealYIndex];

		int displayIndex = 0;

		for (int i = 0; i < cursor.RealXIndex; i++)
		{
			if (currLine[i] == '\t')
			{
				displayIndex+=4; // tab_size=4
				continue;
			}

			displayIndex+=1;
		}

		return displayIndex+1-XScroll; // TODO: figure out why this +1 is needed (because of after last idx maybe?)
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

		// ExitButton.Size.X+BackButton.Size.X+1 (+2 or padding between exit & back buttons)
		// -4 for two spaces and two parentheses (see below AddStr call)
		int maxFileNameLen = WidthBound-viewPos.X-editingMessage.Length-currentFile.Name.Length-(ExitButton.Size.X+BackButton.Size.X+2)-4;

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
				cursorPair.controlling.DisplayY+3,
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

		for (int i = begin.RealYIndex; i < end.RealYIndex; i++)
		{
			Utils.MoveChangeAttr(
				i+3,
				viewPos.X+LongestLineNoLength,
				currLines[i+YScroll].Length,
				CursesAttribute.REVERSE
			);
		}


		Utils.MoveChangeAttr(
			end.DisplayY+3,
			GetEditingDisplayXIndex(end)+viewPos.X+LongestLineNoLength,
			currLines[end.RealYIndex][..end.RealXIndex].Length,
			CursesAttribute.REVERSE
		);

		Utils.MoveChangeAttr(
			begin.DisplayY+3,
			GetEditingDisplayXIndex(begin)+viewPos.X+LongestLineNoLength,
			currLines[begin.RealYIndex][begin.RealXIndex..].Length,
			CursesAttribute.REVERSE
		);
	}

	void DisplayFileContents(Coordinate pos)
	{
		if ((sourceFileAnalyzer is null) || !currentFile!.Name.EndsWith(".cs"))
		{
			for (int i = YScroll; i < NumberOfLines; i++)
			{
				string lineNo = (i+1).ToString();

				NCurses.AttributeOn(Utils.COLOR_PAIR(1));
				AddStr(pos, lineNo);
				NCurses.AttributeOff(Utils.COLOR_PAIR(1));
				// Utils.MoveChangeAttr(pos.Y, pos.X, lineNo.Length, CursesAttribute.NORMAL, 1);

				string tabConverted = currLines[i].Replace("\t", "    ");

				if (XScroll >= tabConverted.Length) continue; // no need to display anything, line is not visible
				


				string toDisplay = tabConverted[XScroll..Math.Min(tabConverted.Length, XScroll+MaxContentDisplayLength)];

				AddStr(pos.AddX(LinePrefixLength), toDisplay);
				
				pos = pos.AddY(1);
			}
			
			return;
		}

		// TODO: fix xScroll for annotated segments
		var annotatedLines = sourceFileAnalyzer.GetAnalyzedLinesAsSourceSegments(
			currentFile.FullName, currLines
		);

		for (int i = YScroll; i < annotatedLines.Length; i++)
		{
			string lineNo = (i+1).ToString();

			NCurses.AttributeOn(Utils.COLOR_PAIR(1));
			AddStr(pos, lineNo);
			NCurses.AttributeOff(Utils.COLOR_PAIR(1));
			// Utils.MoveChangeAttr(pos.Y, pos.X, lineNo.Length, CursesAttribute.NORMAL, 1);

			int currX = LinePrefixLength;
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
			YScroll = Math.Max(0, YScroll-1);

			return;
		}
		else if (Utils.IsMouseEventType(ev, Utils.MOUSE_SCROLL_DOWN))
		{
			YScroll = Math.Min(Math.Max(0, NumberOfLines-(Utils.GetWindowHeight(GlobalScreen.Screen)-3)), YScroll+1);
		
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
			cursors[0].controlling.DisplayY--; // EditingIndex handles the actual logic for moving cursor position within file
			
			return;
		}

		if (key == CursesKey.DOWN)
		{
			cursors[0].controlling.DisplayY++; // EditingIndex handles the actual logic for moving cursor position within file
			
			return;
		}

		if (key == CursesKey.LEFT)
		{
			cursors[0].controlling.DisplayX--; // EditingIndex handles the actual logic for moving cursor position within file
			
			return;
		}

		if (key == CursesKey.RIGHT)
		{
			cursors[0].controlling.DisplayX++; // EditingIndex handles the actual logic for moving cursor position within file
			
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
			if (cursors[0].controlling.RealXIndex == 0)
			{
				if (cursors[0].controlling.RealYIndex == 0) return;
				
				cursors[0].controlling.DisplayX--;

				currLines[cursors[0].controlling.RealYIndex]+=currLines[cursors[0].controlling.RealYIndex+1];
				currLines.RemoveAt(cursors[0].controlling.RealYIndex+1);

				PushChange();

				cursors[0].controlling.DisplayY+=0; // update YScroll

				return;
			}

			cursors[0].controlling.DisplayX--;

			currLines[cursors[0].controlling.RealYIndex] = currLines[cursors[0].controlling.RealYIndex].Remove(cursors[0].controlling.RealXIndex, 1);

			cursors[0].controlling.DisplayY+=0; // update YScroll

			return;
		}

		if (key == '\n')
		{
			string slicedText = currLines[cursors[0].controlling.RealYIndex][cursors[0].controlling.RealXIndex..];
			string leftText = currLines[cursors[0].controlling.RealYIndex][..cursors[0].controlling.RealXIndex];

			currLines[cursors[0].controlling.RealYIndex] = leftText;

			currLines.Insert(cursors[0].controlling.RealYIndex+1, slicedText);
			
			SendKey(CursesKey.DOWN);
			cursors[0].controlling.RealXIndex = 0;

			PushChange();

			return;
		}

		currLines[cursors[0].controlling.RealYIndex] = currLines[cursors[0].controlling.RealYIndex].Insert(cursors[0].controlling.RealXIndex, char.ConvertFromUtf32(key));

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
		editing = false;

		currentFile = file;
		LoadFileIntoCurrLines();

		YScroll = 0;
		XScroll = 0;

		cursors.Clear();
		cursors.Add((new(true, this), new(false, this)));

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