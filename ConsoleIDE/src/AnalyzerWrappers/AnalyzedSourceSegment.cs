using ConsoleIDE.ThemeWrapper;

readonly struct AnalyzedSourceSegment(string text, string type)
{
	public readonly string Text = text;
	public readonly string Type = type;
	public readonly short ColorPair
	{
		get => Theme.GetColorPairNumber(Type);
	}
}