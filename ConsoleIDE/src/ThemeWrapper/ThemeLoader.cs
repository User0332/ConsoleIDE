using Newtonsoft.Json;

namespace ConsoleIDE.ThemeWrapper;

public static class ThemeLoader
{
	static readonly Theme DefaultTheme = new() // TODO: add better colors!
	{
		TypeColor = [306, 788, 690],
		VarColor = [459, 745, 1000],
		MethodColor = [863, 863, 667],
		KeywordColor = [337, 612, 839],
		CommentColor = [416, 600, 333]
	};

	public static Theme LoadThemeFromProjectPathOrDefault(string projectPath) // TODO: have ConsoleIDE configuration dir for global configs
	{
		var theme = DefaultTheme;


		if (File.Exists($"{projectPath}/ConsoleIDE.json"))
		{
			theme = JsonConvert.DeserializeObject<Theme>(File.ReadAllText($"{projectPath}/ConsoleIDE.json")) ?? theme;
		}

		theme.InitColorPairs();

		return theme;
	}
}