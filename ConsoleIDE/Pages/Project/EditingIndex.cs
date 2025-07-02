// A CursorPair is a tuple that either represents a cursor or a selection of text
// the "controlling" cursor is the one that can be moved via the arrow keys, while the nonControlling (if enabled) just delimits the text selection
global using CursorPair = (ConsoleIDE.Pages.Project.EditingIndex controlling, ConsoleIDE.Pages.Project.EditingIndex notControlling);

namespace ConsoleIDE.Pages.Project;

#pragma warning disable CS0660, CS0661
class EditingIndex(bool enabled, FileView parent) // TODO: make this only for primary enabled & controlling cursor
{
	public readonly bool Enabled = enabled;
	public readonly FileView parentFileView = parent;

	private int _displayX;
	private int _displayY;

	/// <summary>
	/// Real x-index of the cursor in the string of the current line
	/// </summary>
	public int RealXIndex
	{
		get;
		set;
	}

	/// <summary>
	/// Zero-indexed line number of the cursor in the current file
	/// </summary>
	public int RealYIndex
	{
		get;
		set;
	}

	/// <summary>
	/// X-coordinate of a cursor on the screen
	/// </summary>
	public int DisplayX
	{
		get
		{
			string currLine = parentFileView.CurrLines[RealYIndex];

			int displayIndex = 0;

			for (int i = 0; i < RealXIndex; i++)
			{
				if (currLine[i] == '\t')
				{
					displayIndex += 4; // tab_size=4
					continue;
				}

				displayIndex++;
			}

			return displayIndex - parentFileView.XScroll + 1;
		}

		private set { _displayX = value; }
	}

	/// <summary>
	/// Y-coordinate of a cursor on the screen
	/// </summary>
	public int DisplayY
	{
		get
		{
			return RealYIndex - parentFileView.YScroll;
		}

		private set { _displayY = value; }	}


	public void IncrementChar()
	{
		string currLine = parentFileView.CurrLines[RealYIndex];

		if (RealXIndex + 1 > currLine.Length) // overflow into next line
		{
			if (RealYIndex + 1 == parentFileView.CurrLines.Count)
			{
				return;
			}

			RealYIndex++;
			RealXIndex = 0;
			parentFileView.XScroll = 0;

			return;
		}

		RealXIndex++;

		UpdateXScroll();
	}

	public void DecrementChar()
	{
		if (RealXIndex == 0) // fall into prev line
		{
			if (RealYIndex == 0)
			{
				if (!IsVisible())
				{
					UpdateXScroll();
					UpdateYScroll();
				}

				return;
			}

			RealYIndex--;
			RealXIndex = parentFileView.CurrLines[RealYIndex].Length;

			UpdateXScroll();

			return;
		}

		RealXIndex--;

		UpdateXScroll();
	}

	public void IncrementLine()
	{
		if (RealYIndex + 1 == parentFileView.CurrLines.Count) // go to end of line
		{
			RealXIndex = parentFileView.CurrLines[RealYIndex].Length;

			UpdateXScroll();

			return;
		}

		RealYIndex++;

		if (RealXIndex > parentFileView.CurrLines[RealYIndex].Length)
		{
			RealXIndex = parentFileView.CurrLines[RealYIndex].Length;
			UpdateXScroll();
		}

		UpdateYScroll();
	}

	public void DecrementLine()
	{
		if (RealYIndex == 0) // go to start of line
		{
			RealXIndex = 0;

			UpdateXScroll();

			if (!IsVisible())
			{
				UpdateYScroll();
			}

			return;
		}

		RealYIndex--;

		if (RealXIndex > parentFileView.CurrLines[RealYIndex].Length)
		{
			RealXIndex = parentFileView.CurrLines[RealYIndex].Length;
			UpdateXScroll();
		}

		UpdateYScroll();
	}

	public void UpdateXScroll()
	{
		if (DisplayX > parentFileView.MaxContentDisplayLength)
		{
			parentFileView.XScroll += DisplayX-parentFileView.MaxContentDisplayLength;
			return;
		}

		if (DisplayX <= 0)
		{
			parentFileView.XScroll = Math.Max(0, parentFileView.XScroll + DisplayX - 1);

			return;
		}
	}

	public void UpdateYScroll()
	{
		if (DisplayY > parentFileView.MaxContentDisplayHeight)
		{
			parentFileView.YScroll += DisplayY-parentFileView.MaxContentDisplayHeight;
			return;
		}

		if (DisplayY < 0)
		{
			parentFileView.YScroll = Math.Max(0, parentFileView.YScroll + DisplayY);
			return;
		}
	}

	public bool IsVisible()
	{
		return IsVisible(DisplayX, DisplayY, parentFileView);
	}

	public static bool IsVisible(int displayX, int displayY, FileView parentFileView)
	{
		return displayY >= 0 && displayY <= parentFileView.MaxContentDisplayHeight && displayX >= 0 && displayX <= parentFileView.MaxContentDisplayLength;

	}


	public static bool operator <(EditingIndex self, EditingIndex other)
	{
		return self.DisplayY < other.DisplayY || (self.RealXIndex < other.RealXIndex) && self.DisplayY == other.DisplayY;
	}

	public static bool operator>(EditingIndex self, EditingIndex other)
	{
		return self.DisplayY > other.DisplayY || (self.RealXIndex > other.RealXIndex) && self.DisplayY == other.DisplayY;
	}

	public bool Equals(EditingIndex other)
	{
		return (RealXIndex == other.RealXIndex) && (DisplayY == other.DisplayY);
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