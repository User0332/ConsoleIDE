namespace ConsoleIDE.AnalyzerWrappers;

enum SourceSegmentType : short
{
	None,
	Type = 2,
	Var,
	Method,
	Keyword,
	Comment
}