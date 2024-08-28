using Newtonsoft.Json;

namespace ConsoleIDE.ThemeWrapper;

public static class ThemeLoader
{
	static readonly Theme DefaultTheme = new() // TODO: add better colors!
	{
		TypeColor = [0, 500, 0],
		VarColor = [0, 250, 250],
		MethodColor = [250, 250, 0],
		KeywordColor = [0, 0, 250],
		CommentColor = [250, 500, 250]
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