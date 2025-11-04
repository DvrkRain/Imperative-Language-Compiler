using Data.Objects;
using Data.IO;
using LexicalAnalyzer;
using AST;

namespace Compiler
{
	class Program
	{
		static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Usage: compiler-csharp <source-file-path> <tree-option>");
				return;
			}

			string filepath = args[0];
			FileReader reader = new FileReader(filepath);
			Console.WriteLine("Filename: " + reader.filename);

			// Lexic analysis
			Queue<Token> stream = new Queue<Token>();
			Lexer lexer = new Lexer(reader);
			lexer.ParseFile(ref stream);

			int treeOption = int.Parse(args[1]);
			if (treeOption == 0) {
				while (stream.Count > 0) {
					Token token = stream.Dequeue();
					token.PrintInfo();
				}

				return;
			}

			// Syntax analysis
			ProgramNode AST = new ProgramNode(new Position(0,0));
			AST.Parse(ref stream);

			if (treeOption == 1) AST.PrintInfo("");

			// Semantic analysis
		}
	}
}
