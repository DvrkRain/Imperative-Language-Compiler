using Data.Objects;
using Data.IO;
using Data.ErrorHandling;

using LexicalAnalyzer;
using AST;
using SemanticAnalyzer.SymbolTable;

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
            
            // Read file
			string filepath = args[0];
			FileReader reader = new FileReader(filepath);
			Console.WriteLine("Filename: " + reader.filename);

			// Lexic analysis
            ErrorHandling.ChangeStage("Lexical analysis");
			Queue<Token> stream = new Queue<Token>();
			Lexer lexer = new Lexer(reader);
			lexer.ParseFile(ref stream);

            if (ErrorHandling.Count() > 0) {
                ErrorHandling.PrintErrors();
                return;
            }

            int treeOption = int.Parse(args[1]);
            if (treeOption == 0) {
                while (stream.Count > 0) {
                    Token token = stream.Dequeue();
                    token.PrintInfo();
                }

                return;
            }

            // Syntax analysis
			ErrorHandling.ChangeStage("Syntax analysis");
			ProgramNode AST = new ProgramNode(new Position(0,0));
			AST.Parse(ref stream);
            
            if (ErrorHandling.Count() > 0) {
                Console.WriteLine("Syntax Analysis errors:");
                ErrorHandling.PrintErrors();
                return;
            }

			if (treeOption == 1) AST.PrintInfo("");

			// Semantic analysis
            SymbolTable.InitializeSymbolTable();
			ErrorHandling.ChangeStage("Semantic analysis");
		}
	}
}
