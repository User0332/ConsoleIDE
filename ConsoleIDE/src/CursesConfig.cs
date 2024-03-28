namespace ConsoleIDE;

class CursesConfig : CursesLibraryNames
{
	public override bool ReplaceWindowsDefaults => true;
	public override List<string> NamesWindows =>
		new() {
			"libncursesw6.dll",
		};
}