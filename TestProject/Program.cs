/*
	This C# program writes "Hello, World!" to
	the standard output stream via Console.WriteLine.
	Made with ConsoleIDE.
*/

public class Program
{
	static int Main()
	{
		Console.WriteLine("Hello, World!");

		Method();

		return 0;
	}

	static void Method()
	{
		Console.WriteLine("Hello World (from Program.Method)!");
	}
}
