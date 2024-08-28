using ConsoleIDE.AnalyzerWrappers;

namespace ConsoleIDE.ThemeWrapper;

public class Theme
{
	public required short[] TypeColor { get; set; }
	public required short[] VarColor { get; set; }
	public required short[] MethodColor { get; set; }
	public required short[] KeywordColor { get; set; }
	public required short[] CommentColor { get; set; }

	public void InitColorPairs()
	{
		InitColorPair(((short) SourceSegmentType.Type)*10, (short) SourceSegmentType.Type, TypeColor);
		InitColorPair(((short) SourceSegmentType.Var)*10, (short) SourceSegmentType.Var, VarColor);
		InitColorPair(((short) SourceSegmentType.Method)*10, (short) SourceSegmentType.Method, MethodColor);
		InitColorPair(((short) SourceSegmentType.Keyword)*10, (short) SourceSegmentType.Keyword, KeywordColor);
		InitColorPair(((short) SourceSegmentType.Comment)*10, (short) SourceSegmentType.Comment, CommentColor);
	}

	static void InitColorPair(short useColorNum, short usePairNum, short[] color)
	{

		NCurses.InitColor(useColorNum, color[0], color[1], color[2]);
		NCurses.InitPair(usePairNum, useColorNum, CursesColor.BLACK);
	}
}