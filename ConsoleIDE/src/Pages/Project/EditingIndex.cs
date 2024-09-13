// A CursorPair is a tuple that either represents a cursor or a selection of text
// the "controlling" cursor is the one that can be moved via the arrow keys, while the nonControlling (if enabled) just delimits the text selection
global using CursorPair = (ConsoleIDE.Pages.Project.EditingIndex controlling, ConsoleIDE.Pages.Project.EditingIndex notControlling);

namespace ConsoleIDE.Pages.Project;

#pragma warning disable CS0660, CS0661
class EditingIndex(bool enabled, FileView parent) // TODO: make this only for primary enabled & controlling cursor
{
	public readonly bool Enabled = enabled;
	public readonly FileView parentFileView = parent;

	public int RealXIndex;

	public int RealYIndex;

	public int DisplayX
	{
		get => RealXIndex-parentFileView.XScroll;

		set
		{
			int change = value-DisplayX;

			RealXIndex+=change;

			int diff;

			if ((diff = RealXIndex-parentFileView.CurrentLineLength-1) >= 0) // spill over `diff` chars into next line, we don't do >= because index should be able to be = to line length (e.g. index can be pointing past the last char)
			{
				if (RealYIndex < parentFileView.NumberOfLines-1)
				{
					RealYIndex++;
					RealXIndex = diff;

					#pragma warning disable CA2011, CA2245 // assigning prop to itself warnings
					DisplayX = DisplayX; // update XScroll & perform X bounding
					DisplayY = DisplayY; // update YScroll & perform Y bounding
					#pragma warning restore CA2011, CA2245

					return;
				}

				RealXIndex = parentFileView.CurrentLineLength;

				return;
			}

			if ((diff = -RealXIndex-1) >= 0) // spill back `diff` chars into prev line
			{
				if (RealYIndex > 0)
				{
					RealYIndex--;
					RealXIndex = parentFileView.CurrentLineLength-diff;

					#pragma warning disable CA2011, CA2245 // assigning prop to itself warnings
					DisplayX = DisplayX; // update XScroll & perform X bounding
					DisplayY = DisplayY; // update YScroll & perform Y bounding
					#pragma warning restore CA2011, CA2245

					return;
				}

				RealXIndex = 0;

				return;
			}

			if ((diff = -DisplayX) > 0)
			{
				parentFileView.XScroll-=diff; // we need to scroll right! (spillback handled by above if case)
				
				return;
			}

			if ((diff = DisplayX-(parentFileView.MaxContentDisplayLength)) > 0)
			{
				// this should theoretically never reach MaxXScroll because any change that would cause a spill into the next line will be handled by the index > line length check
				parentFileView.XScroll+=diff; // we are at the far right of the screen, scroll right
				return;
			}
		}
	}

	public int DisplayY
	{
		get => RealYIndex-parentFileView.YScroll;

		set
		{
			int change = value-DisplayY;

			RealYIndex+=change;

			int diff;

			if (RealYIndex > parentFileView.NumberOfLines-1) // stop cursor from going past last line
			{
				RealYIndex = parentFileView.NumberOfLines-1;;
				RealXIndex = parentFileView.CurrentLineLength;

				#pragma warning disable CA2011, CA2245 // assigning prop to itself warnings
				DisplayX = DisplayX; // update XScroll
				DisplayY = DisplayY; // update YScroll
				#pragma warning restore CA2011, CA2245

				return;
			}

			if (RealYIndex < 0) // stop cursor from going past line 1
			{
				RealYIndex = 0;
				RealXIndex = 0;

				#pragma warning disable CA2011, CA2245 // assigning prop to itself warnings
				DisplayX = DisplayX; // update XScroll
				DisplayY = DisplayY; // update YScroll
				#pragma warning restore CA2011, CA2245

				return;
			}

			if ((diff = RealXIndex-parentFileView.CurrentLineLength) > 0) // update x to be on end of line if we move to a shorter line
			{
				RealXIndex-=diff;

				#pragma warning disable CA2011, CA2245 // assigning prop to itself warnings
				DisplayX = DisplayX; // update XScroll
				#pragma warning restore CA2011, CA2245
			}

			if ((diff = -DisplayY) > 0)
			{
				parentFileView.YScroll-=diff; // we need to scroll up!

				return;
			}

			if ((diff = DisplayY-parentFileView.MaxContentDisplayHeight) > 0)
			{
				parentFileView.YScroll+=diff; // we are at the bottom, scroll down
				
				return;
			}
		}
	}



	public static bool operator<(EditingIndex self, EditingIndex other)
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