using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace ConsoleIDE.AnalyzerWrappers;

static class SourceFileAnalyzer
{
	public static List<List<AnalyzedSourceSegment>> GetAnalyzedLinesAsSourceSegments(string projectFile, string fromSourceFile, string[] currentSourceLines)
	{
		var workspace = MSBuildWorkspace.Create();
		var project = workspace.OpenProjectAsync(projectFile).Result;
		var compilationWithOldTree = project.GetCompilationAsync().Result!;

		var newTree = CSharpSyntaxTree.ParseText(string.Join('\n', currentSourceLines), path: fromSourceFile);

		var compilation = compilationWithOldTree.ReplaceSyntaxTree(
			compilationWithOldTree.SyntaxTrees.Where(tree => tree.FilePath.Equals(fromSourceFile)).First(),
			newTree
		);

		var semanticModel = compilation.GetSemanticModel(newTree);
		var root = newTree.GetRoot();

		// TODO: get semantic info

		List<List<AnalyzedSourceSegment>> lines = new(10);

		for (int i = 0; i < currentSourceLines.Length; i++) lines.Add(new(currentSourceLines[i].Length));

		var tokens = root.DescendantTokens();
	
		foreach (var token in tokens)
		{
			var symbol = semanticModel.GetSymbolInfo(token.Parent!).Symbol;

			string internalType = "none";

			if (SyntaxFacts.IsKeywordKind(token.Kind()))
			{
				internalType = "keyword";
			}
			else if (symbol is not null)
			{
				internalType = GetInternalSymbolType(symbol);
			}

			var lineNo = token.GetLocation().GetLineSpan().StartLinePosition.Line;

			lines[lineNo].Add(new(token.Text, internalType));
		}

		return lines;
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
				SymbolKind.FunctionPointerType => "type",

			SymbolKind.Field or
				SymbolKind.Local or 
				SymbolKind.Parameter or 
				SymbolKind.RangeVariable => "var",

			SymbolKind.Method => "method",

			_ => "none"
		};
	}
}