using ConsoleIDE.AnalyzerWrappers;

readonly struct AnalyzedSourceSegment(string text, SourceSegmentType type, int charPos)
{
	public readonly string Text = text;
	public readonly SourceSegmentType Type = type;
	public readonly short ColorPairNumber = (short) type;
	public readonly int CharPos = charPos;
}