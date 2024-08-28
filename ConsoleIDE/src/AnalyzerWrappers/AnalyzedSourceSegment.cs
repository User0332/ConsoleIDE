using ConsoleIDE.AnalyzerWrappers;

readonly struct AnalyzedSourceSegment(string text, SourceSegmentType type)
{
	public readonly string Text = text;
	public readonly SourceSegmentType Type = type;
	public readonly short ColorPairNumber = (short) type;
}