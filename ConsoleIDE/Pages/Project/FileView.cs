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
	bool editing = false;
	bool saved = true;
	int secSinceLastChange = DateTime.Now.Second;
	public List<string> CurrLines = [];
	public int YScroll;
	public int XScroll;
	public int MaxYScroll => NumberOfLines-MaxContentDisplayHeight;
	public int MaxXScroll => LongestLineLength-MaxContentDisplayLength;
	public readonly int WidthBound = widthBound;
	public readonly int HeightBound = Utils.GetWindowHeight(GlobalScreen.Screen); // todo: paramaterize
	public int CurrentLineLength => CurrLines[cursors[0].controlling.RealYIndex].Length;
	int LongestLineNoLength => NumberOfLines.ToString().Length;
	int LinePrefixLength => LongestLineNoLength+1; // longest line no + one space for extra padding
	int LongestLineLength => CurrLines.Max(line => line.Length);
	readonly int FilePrefixHeight = 3;
	public int NumberOfLines => CurrLines.Count;
	public int MaxContentDisplayLength => WidthBound-LinePrefixLength-viewPos.X;
	public int MaxContentDisplayHeight => HeightBound-FilePrefixHeight-viewPos.Y-1;

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

		DisplayFileContents(viewPos.AddY(FilePrefixHeight));

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
				cursorPair.controlling.DisplayY+viewPos.Y+FilePrefixHeight,
				cursorPair.controlling.DisplayX+viewPos.X+LongestLineNoLength,
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
				CurrLines[i+YScroll].Length,
				CursesAttribute.REVERSE
			);
		}


		Utils.MoveChangeAttr(
			end.DisplayY+viewPos.Y+FilePrefixHeight,
			end.DisplayX+viewPos.X+LongestLineNoLength,
			CurrLines[end.RealYIndex][..end.RealXIndex].Length,
			CursesAttribute.REVERSE
		);

		Utils.MoveChangeAttr(
			begin.DisplayY+viewPos.Y+FilePrefixHeight,
			end.DisplayX+viewPos.X+LongestLineNoLength,
			CurrLines[begin.RealYIndex][begin.RealXIndex..].Length,
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

				string tabConverted = CurrLines[i].Replace("\t", "    ");

				if (XScroll >= tabConverted.Length) continue; // no need to display anything, line is not visible
				
				string toDisplay = tabConverted[XScroll..Math.Min(tabConverted.Length, XScroll+MaxContentDisplayLength)];

				AddStr(pos.AddX(LinePrefixLength), toDisplay);
				
				pos = pos.AddY(1);
			}
			
			return;
		}

		// TODO: fix xScroll for annotated segments
		var annotatedLines = sourceFileAnalyzer.GetAnalyzedLinesAsSourceSegments(
			currentFile.FullName, CurrLines
		);

		for (int i = YScroll; i < annotatedLines.Length; i++)
		{
			string lineNo = (i+1).ToString();

			NCurses.AttributeOn(Utils.COLOR_PAIR(1));
			AddStr(pos, lineNo);
			NCurses.AttributeOff(Utils.COLOR_PAIR(1));
			// Utils.MoveChangeAttr(pos.Y, pos.X, lineNo.Length, CursesAttribute.NORMAL, 1);

			int currX = LinePrefixLength;

			// find the first segment that should be displayed

			int searchX = 0;

			int charsToDisplayFromFirstSegment = 0;
			int firstSegmentIndex = -1;

			for (int j = 0; j < annotatedLines[i].Length; j++)
			{
				searchX += annotatedLines[i][j].Text.Replace("\t", "    ").Length;

				if (searchX > XScroll)
				{
					firstSegmentIndex = j;
					charsToDisplayFromFirstSegment = searchX - XScroll;
					break;
				}
			}

			if (firstSegmentIndex == -1)
			{
				pos = pos.AddY(1);
				continue;
			}

			// display first annotated segment

			var firstSegment = annotatedLines[i][firstSegmentIndex];

			var firstColoredAttr = Utils.COLOR_PAIR(firstSegment.ColorPairNumber);

			var firstDisplayTextUnsliced = firstSegment.Text.Replace("\t", "    ");

			var start = firstDisplayTextUnsliced.Length - charsToDisplayFromFirstSegment;
			var firstDisplayText = firstDisplayTextUnsliced[start..Math.Min(start+MaxContentDisplayLength, firstDisplayTextUnsliced.Length)];

			NCurses.AttributeOn(firstColoredAttr);
			AddStr(pos.AddX(currX), firstDisplayText);
			NCurses.AttributeOff(firstColoredAttr);

			currX += firstDisplayText.Length;

			if (currX < MaxContentDisplayLength)
			{
				for (int j = firstSegmentIndex + 1; j < annotatedLines[i].Length; j++)
				{
					var segment = annotatedLines[i][j];

					var coloredAttr = Utils.COLOR_PAIR(segment.ColorPairNumber);

					var displayText = segment.Text.Replace("\t", "    ");

					int excessLength = (displayText.Length + currX) - MaxContentDisplayLength;

					if (excessLength > 0)
					{
						displayText = displayText[..(displayText.Length - excessLength + 1)];
					}

					NCurses.AttributeOn(coloredAttr);
					AddStr(pos.AddX(currX), displayText);
					NCurses.AttributeOff(coloredAttr);

					if (excessLength > 0) break;

					currX += displayText.Length;
				}
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
			
			File.WriteAllLines(currentFile.FullName, [.. CurrLines]);
			return;
		}

		if (!editing) return;


		if (key == CursesKey.UP)
		{
			cursors[0].controlling.DecrementLine();
			
			return;
		}

		if (key == CursesKey.DOWN)
		{
			cursors[0].controlling.IncrementLine();
			
			return;
		}

		if (key == CursesKey.LEFT)
		{
			cursors[0].controlling.DecrementChar();
			
			return;
		}

		if (key == CursesKey.RIGHT)
		{
			cursors[0].controlling.IncrementChar();
			
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
			if (cursors[0].controlling.RealXIndex == 0) // delete the line
			{
				if (cursors[0].controlling.RealYIndex == 0) return;

				cursors[0].controlling.DecrementLine();
				cursors[0].controlling.RealXIndex = CurrentLineLength;
				cursors[0].controlling.UpdateXScroll();

				CurrLines[cursors[0].controlling.RealYIndex]+=CurrLines[cursors[0].controlling.RealYIndex+1];
				CurrLines.RemoveAt(cursors[0].controlling.RealYIndex+1);

				PushChange();

				return;
			}

			cursors[0].controlling.DecrementChar();

			CurrLines[cursors[0].controlling.RealYIndex] = CurrLines[cursors[0].controlling.RealYIndex].Remove(cursors[0].controlling.RealXIndex, 1);

			PushChange();

			return;
		}

		if (key == '\n')
		{
			string slicedText = CurrLines[cursors[0].controlling.RealYIndex][cursors[0].controlling.RealXIndex..];
			string leftText = CurrLines[cursors[0].controlling.RealYIndex][..cursors[0].controlling.RealXIndex];

			CurrLines[cursors[0].controlling.RealYIndex] = leftText;

			CurrLines.Insert(cursors[0].controlling.RealYIndex+1, slicedText);

			cursors[0].controlling.IncrementLine();
			
			cursors[0].controlling.RealXIndex = 0;
			cursors[0].controlling.UpdateXScroll();

			PushChange();

			return;
		}

		var charToAdd = char.ConvertFromUtf32(key);

		CurrLines[cursors[0].controlling.RealYIndex] = CurrLines[cursors[0].controlling.RealYIndex].Insert(cursors[0].controlling.RealXIndex, charToAdd);
		
		cursors[0].controlling.IncrementChar();

		if ((DateTime.Now.Second - secSinceLastChange) >= 20) PushChange();
	}

	void LoadFileIntoCurrLines()
	{
		var lines = File.ReadAllLines(currentFile!.FullName);

		if (lines.Length == 0)
		{
			CurrLines = [""];
			return;
		}

		CurrLines = [..lines];
	}

	public void PushChange()
	{
		if (!undos[^1].lines.SequenceEqual(CurrLines)) // has a change been made?
		{
			undos.Add(([..CurrLines], cursors[0]));

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

		redos.Add(([..CurrLines], cursors[0]));

		cursors.Clear();
		cursors.Add(new());

		(CurrLines, cursors[0]) = undos[^1];

		if (undos.Count == 1) return;

		undos.RemoveAt(undos.Count-1);
	}

	public void TryRedo()
	{
		if (redos.Count == 0) return;

		saved = false;

		undos.Add(([..CurrLines], cursors[0]));
		
		cursors.Clear();
		cursors.Add(new());

		(CurrLines, cursors[0]) = redos[^1];

		redos.RemoveAt(redos.Count-1);		
	}

	public void ToggleEditingMode()
	{
		editing = !editing;
	}

	public void ChangeTo(FileInfo file)
	{
		editing = false;
		saved = true;

		currentFile = file;
		LoadFileIntoCurrLines();

		YScroll = 0;
		XScroll = 0;

		cursors.Clear();
		cursors.Add((new(true, this), new(false, this)));

		cursors[0].controlling.RealXIndex = 0;
		cursors[0].controlling.RealYIndex = 0;

		undos.Clear();
		undos.Add(([..CurrLines], cursors[0]));
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