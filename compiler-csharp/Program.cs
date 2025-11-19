using Data.Objects;
using Data.IO;
using Data.ErrorHandling;

using LexicalAnalyzer;
using AST;
using SemanticAnalyzer.SymbolTable;

#pragma warning disable

namespace Compiler
{
	class Program
	{
        static void Main(string[] args) {
            if (args.Length < 2) {
                Console.WriteLine("Usage: compiler-csharp <source-file-path> <tree-option>");
                return;
            }

            // Read args[0] file
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
                Environment.Exit(1);
                return;
            }

            int treeOption = int.Parse(args[1]);
            
            // If args[1] == 0 print tokens
            if (treeOption == 0) {
                while (stream.Count > 0) {
                    Token token = stream.Dequeue();
                    token.PrintInfo();
                }

                return;
            }

            // Syntax analysis
            ErrorHandling.ChangeStage("Syntax analysis");
            ProgramNode AST = new ProgramNode(new Position(0, 0), main: true);
            AST.Parse(ref stream);

            if (ErrorHandling.Count() > 0) {
                ErrorHandling.PrintErrors();
                Environment.Exit(1);
                return;
            }

            // If args[1] == 1 print syntax ast tree
            if (treeOption == 1) {
                AST.PrintInfo("");
                return;
            }

            // Semantic analysis
            SymbolTable.InitializeSymbolTable();
            ErrorHandling.ChangeStage("Semantic analysis");
            AST.Verify();
            
            if (ErrorHandling.Count() > 0) {
                AST.PrintInfo("");
                ErrorHandling.PrintErrors();
                Environment.Exit(1);
                return;
            }

            // If args[1] == 2 print semantic ast tree
            if (treeOption == 2) {
                AST.PrintInfo("");
                return;
            }
            
            
            // Code generation
            ErrorHandling.ChangeStage("Code generation");

            string outputFileName = args.Length > 2 ? args[2] : "output.dll";

            var codeGen = new CodeGen.CodeGenContext("CompiledProgram");

            try {
                AST.Generate(codeGen);
                codeGen.Save(outputFileName);
                Console.WriteLine($"Code generation successful. Output: {outputFileName}");
            } catch (Exception ex) {
                ErrorHandling.Add("CodeGen", new Position(0, 0), $"Code generation failed: {ex.Message}");
                ErrorHandling.PrintErrors();
                Environment.Exit(1);
            }
        }
    }
}
