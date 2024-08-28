using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace ConsoleIDE.AnalyzerWrappers;

class SourceFileAnalyzer
{
	readonly MSBuildWorkspace workspace; 
	readonly Project project;
	readonly Compilation compilationWithAllTrees;

	public SourceFileAnalyzer(string projectFile)
	{
		workspace = MSBuildWorkspace.Create();
		project = workspace.OpenProjectAsync(projectFile).Result;
		compilationWithAllTrees = project.GetCompilationAsync().Result!;
	}
	public AnalyzedSourceSegment[][] GetAnalyzedLinesAsSourceSegments(string fromSourceFile, List<string> currentSourceLines)
	{
		var newTree = CSharpSyntaxTree.ParseText(string.Join('\n', currentSourceLines), path: fromSourceFile);

		var compilation = compilationWithAllTrees.ReplaceSyntaxTree(
			compilationWithAllTrees.SyntaxTrees.Where(tree => tree.FilePath.Equals(fromSourceFile)).First(),
			newTree
		);

		var semanticModel = compilation.GetSemanticModel(newTree);
		var root = newTree.GetRoot();

		List<List<AnalyzedSourceSegment>> lines = new(currentSourceLines.Count);

		for (int i = 0; i < currentSourceLines.Count; i++) lines.Add(new(10));

		var tokens = root.DescendantTokens();

		foreach (var token in tokens)
		{
			if (token.IsKind(SyntaxKind.EndOfFileToken)) break;

			bool isKeyword = false;
			bool isStringOrCharLiteral = false;
			bool isNumericalLiteral = false;

			var lineNo = token.GetLocation().GetLineSpan().StartLinePosition.Line;
			var charPos = token.GetLocation().GetLineSpan().StartLinePosition.Character;

			if (
				!(
					token.IsKind(SyntaxKind.IdentifierName) ||
					token.IsKind(SyntaxKind.IdentifierToken) ||
					(isKeyword = SyntaxFacts.IsKeywordKind(token.Kind())) ||
					(isStringOrCharLiteral = SyntaxFacts.IsLiteralExpression(token.Kind()) && !token.IsKind(SyntaxKind.NumericLiteralToken)) ||
					(isNumericalLiteral = token.IsKind(SyntaxKind.NumericLiteralToken))
				)
			)
			{
				lines[lineNo].Add(new(token.Text, SourceSegmentType.None, charPos));
				continue;
			}

			SourceSegmentType internalType = SourceSegmentType.None;
			
			var declSymbol = semanticModel.GetDeclaredSymbol(token.Parent!);
			var symbol = semanticModel.GetSymbolInfo(token.Parent!).Symbol;

			if (isKeyword) internalType = SourceSegmentType.Keyword;
			else if (isStringOrCharLiteral)
			{
				bool isMultiline = token.IsKind(SyntaxKind.MultiLineRawStringLiteralToken) && token.IsKind(SyntaxKind.Utf8MultiLineRawStringLiteralToken);

				if (isMultiline)
				{
					var stringLines = token.Text.Split('\n');

					lines[lineNo].Add(new(stringLines[0], SourceSegmentType.Comment, charPos));

					for (int i = 1; i < stringLines.Length; i++)
					{
						lines[lineNo+i].Add(new(stringLines[i], SourceSegmentType.Comment, 0));
					}
					
					continue;
				}

				internalType = SourceSegmentType.StringLiteral;
			}
			else if (isNumericalLiteral) internalType = SourceSegmentType.NumericalLiteral;
			else if (symbol is not null) internalType = GetInternalSymbolType(symbol);
			else if (declSymbol is not null) internalType = GetInternalSymbolType(declSymbol);

			lines[lineNo].Add(new(token.Text, internalType, charPos));
		}

		var trivia = root.DescendantTrivia();

		PlaceTrivia(lines, trivia);

		return lines.Select(
			line => line.OrderBy(
				segment => segment.CharPos
			).ToArray()
		).ToArray(); // TODO: optimize this by not re-ordering at the end and instead ordering while building it
	}
	
	static void PlaceTrivia(List<List<AnalyzedSourceSegment>> lines, IEnumerable<SyntaxTrivia> triviaList)
	{
		foreach (var trivia in FilterNewLinesFromTrivia(triviaList)) // TODO: group trivia by line so we have less segments (e.g. we can use string.Join)
		{
			var lineNo = trivia.GetLocation().GetLineSpan().StartLinePosition.Line;
			var charPos = trivia.GetLocation().GetLineSpan().StartLinePosition.Character;

			var isSingleLineComment =
				trivia.IsKind(SyntaxKind.SingleLineCommentTrivia) ||
				trivia.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia);

			var isMultiLineComment = trivia.IsKind(SyntaxKind.MultiLineCommentTrivia) ||
				trivia.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia);

			if (isMultiLineComment)
			{
				var commentLines = trivia.ToString().Split('\n');

				lines[lineNo].Add(new(commentLines[0], SourceSegmentType.Comment, charPos));

				for (int i = 1; i < commentLines.Length; i++)
				{
					lines[lineNo+i].Add(new(commentLines[i], SourceSegmentType.Comment, 0));
				}
			}
			else
			{
				var segmentType = isSingleLineComment ? SourceSegmentType.Comment : SourceSegmentType.None;

				lines[lineNo].Add(new(trivia.ToString(), segmentType, charPos));
			}
		}
	}

	static IEnumerable<SyntaxTrivia> FilterNewLinesFromTrivia(IEnumerable<SyntaxTrivia> trivia)
	{
		return trivia.Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia));
	}

	static SourceSegmentType GetInternalSymbolType(ISymbol symbol)
	{
		return symbol.Kind switch
		{
			SymbolKind.ArrayType or
				SymbolKind.DynamicType or
				SymbolKind.NamedType or
				SymbolKind.TypeParameter or
				SymbolKind.PointerType or
				SymbolKind.FunctionPointerType or
				SymbolKind.Namespace or
				SymbolKind.Alias  => SourceSegmentType.Type,

			SymbolKind.Field or
				SymbolKind.Local or 
				SymbolKind.Parameter or 
				SymbolKind.RangeVariable or
				SymbolKind.Property => SourceSegmentType.Var,

			SymbolKind.Method => SourceSegmentType.Method,

			SymbolKind.Preprocessing or
				SymbolKind.Discard => SourceSegmentType.Keyword,

			SymbolKind.Label or
				SymbolKind.ErrorType or
				SymbolKind.Assembly or
				SymbolKind.Event or
				SymbolKind.NetModule => SourceSegmentType.None,

			var other => throw new Exception(other.ToString())
		};
	}
}