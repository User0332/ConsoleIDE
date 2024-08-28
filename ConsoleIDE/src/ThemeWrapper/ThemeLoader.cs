using Newtonsoft.Json;

namespace ConsoleIDE.ThemeWrapper;

public static class ThemeLoader
{
	static readonly Theme DefaultTheme = new() // TODO: add better colors!
	{
		TypeColor = [0, 500, 0],
		VarColor = [0, 255, 255],
		MethodColor = [255, 255, 0],
		KeywordColor = [0, 0, 255]
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