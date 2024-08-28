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
	public List<List<AnalyzedSourceSegment>> GetAnalyzedLinesAsSourceSegments(string fromSourceFile, List<string> currentSourceLines)
	{
		var newTree = CSharpSyntaxTree.ParseText(string.Join('\n', currentSourceLines), path: fromSourceFile);

		var compilation = compilationWithAllTrees.ReplaceSyntaxTree(
			compilationWithAllTrees.SyntaxTrees.Where(tree => tree.FilePath.Equals(fromSourceFile)).First(),
			newTree
		);

		var semanticModel = compilation.GetSemanticModel(newTree);
		var root = newTree.GetRoot();

		List<List<AnalyzedSourceSegment>> lines = new(10);

		for (int i = 0; i < currentSourceLines.Count; i++) lines.Add(new(currentSourceLines[i].Length));

		var tokens = root.DescendantTokens();
	
		foreach (var token in tokens)
		{
			var declSymbol = semanticModel.GetDeclaredSymbol(token.Parent!);
			var symbol = semanticModel.GetSymbolInfo(token.Parent!).Symbol;
			var type = semanticModel.GetTypeInfo(token.Parent!).Type;

			string internalType = "none";

			if (SyntaxFacts.IsKeywordKind(token.Kind()))
			{
				internalType = "keyword";
			}
			else if (symbol is not null)
			{
				internalType = GetInternalSymbolType(symbol);
			}
			else if (type is not null)
			{
				internalType = "type";
			}
			else if (declSymbol is not null)
			{
				internalType = GetInternalSymbolType(declSymbol);
			}

			var lineNo = token.GetLocation().GetLineSpan().StartLinePosition.Line;

			lines[lineNo].Add(new(string.Join("", FilterNewLinesFromTrivia(token.LeadingTrivia)), "none"));
			lines[lineNo].Add(new(token.Text, internalType));
			lines[lineNo].Add(new(string.Join("", FilterNewLinesFromTrivia(token.TrailingTrivia)), "none"));
		}

		return lines;
	}

	static IEnumerable<SyntaxTrivia> FilterNewLinesFromTrivia(IEnumerable<SyntaxTrivia> trivia)
	{
		return trivia.Where(t => !t.IsKind(SyntaxKind.EndOfLineTrivia));
	}

	static string GetInternalSymbolType(ISymbol symbol)
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
				SymbolKind.Alias => "type",

			SymbolKind.Field or
				SymbolKind.Local or 
				SymbolKind.Parameter or 
				SymbolKind.RangeVariable or
				SymbolKind.Property or
				SymbolKind.Label => "var",

			SymbolKind.Method => "method",

			var other => throw new Exception(other.ToString())
		};
	}
}