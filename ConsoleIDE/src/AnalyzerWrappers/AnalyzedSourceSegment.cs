using ConsoleIDE.ThemeWrapper;

readonly struct AnalyzedSourceSegment
{
	public readonly string Text;
	public readonly string Type;
	public readonly short ColorPair
	{
		get => Theme.GetColorPairNumber(Type);
	}

}