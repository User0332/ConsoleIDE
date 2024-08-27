namespace ConsoleIDE.ThemeWrapper;

public class Theme
{
	const short THEME_COLOR_TYPE = 256;
	const short THEME_COLOR_VAR = 257;
	const short THEME_COLOR_METHOD = 258;
	const short THEME_COLOR_KEYWORD = 259;

	public const short THEME_COLOR_PAIR_TYPE = THEME_COLOR_TYPE*2;
	public const short THEME_COLOR_PAIR_VAR = THEME_COLOR_VAR*2;
	public const short THEME_COLOR_PAIR_METHOD = THEME_COLOR_METHOD*2;
	public const short THEME_COLOR_PAIR_KEYWORD = THEME_COLOR_KEYWORD*2;

	public required short[] TypeColor { get; set; }
	public required short[] VarColor { get; set; }
	public required short[] MethodColor { get; set; }
	public required short[] KeywordColor { get; set; }

	public void InitColorPairs()
	{
		InitColorPair(THEME_COLOR_TYPE, THEME_COLOR_PAIR_TYPE, TypeColor);
		InitColorPair(THEME_COLOR_VAR, THEME_COLOR_PAIR_VAR, VarColor);
		InitColorPair(THEME_COLOR_METHOD, THEME_COLOR_PAIR_METHOD, MethodColor);
		InitColorPair(THEME_COLOR_KEYWORD, THEME_COLOR_PAIR_KEYWORD, KeywordColor);
	}

	static void InitColorPair(short useColorNum, short usePairNum, short[] color)
	{
		NCurses.InitColor(useColorNum, color[0], color[1], color[2]);
		NCurses.InitPair(usePairNum, useColorNum, CursesColor.BLACK);
	}

	public static short GetColorPairNumber(string symbolType)
	{
		return symbolType switch
		{
			"type" => THEME_COLOR_PAIR_TYPE,
			"var" => THEME_COLOR_PAIR_VAR,
			"method" => THEME_COLOR_PAIR_METHOD,
			"keyword" => THEME_COLOR_PAIR_KEYWORD,
			_ => throw new ArgumentException("How did we get here?"),
		};
	}
}