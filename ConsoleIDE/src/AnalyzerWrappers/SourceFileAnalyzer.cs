using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.MSBuild;

namespace ConsoleIDE.AnalyzerWrappers;

static class SourceFileAnalyzer
{
	public static List<List<AnalyzedSourceSegment>> GetAnalyzedLinesAsSourceSegments(string projectFile, string fromSourceFile, string currentSource)
	{
		var workspace = MSBuildWorkspace.Create();
		var project = workspace.OpenProjectAsync(projectFile).Result;
		var compilationWithOldTree = project.GetCompilationAsync().Result!;

		var newTree = CSharpSyntaxTree.ParseText(currentSource, path: fromSourceFile);

		var compilation = compilationWithOldTree.ReplaceSyntaxTree(
			compilationWithOldTree.SyntaxTrees.Where(tree => tree.FilePath.Equals(fromSourceFile)).First(),
			newTree
		);

		var semanticModel = compilation.GetSemanticModel(newTree);

		// TODO: get semantic info
	}
}