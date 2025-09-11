using LexicalAnalyzer.IO;
using LexicalAnalyzer.TokenTree;
using LexicalAnalyzer;

namespace Compiler
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length < 1)
			{
				Console.WriteLine("Usage: compiler-csharp <source-file-path>");
				return;
			}

			string filepath = args[0];
			FileReader reader = new FileReader(filepath);
			Console.WriteLine("Filename: " + reader.filename);

			Queue<Token> stream = new Queue<Token>();

			Lexer lexer = new Lexer(reader, stream);
			lexer.ParseFile();

			while(stream.Count > 0) {
				Token current = stream.Dequeue();
				current.PrintInfo();
			}
		}
	}
}
