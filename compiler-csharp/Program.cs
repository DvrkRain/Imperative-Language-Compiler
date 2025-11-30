using System.Reflection;
using System.Runtime.Loader;
using Data.Objects;
using Data.IO;
using Data.ErrorHandling;

using LexicalAnalyzer;
using AST;
using SemanticAnalyzer.SymbolTable;

#pragma warning disable

namespace Compiler;
class Program {
	static void Main(string[] args) {
		Arguments arguments = CLIParser.ParseArgs(args);
		// Console.WriteLine(arguments.ToString());
		if(!arguments.valid) return;

		FileReader.SetFile(arguments.inputFile);
		Console.WriteLine("Filename: " + FileReader.filename);

		// Lexic analysis
		ErrorHandling.ChangeStage("Lexical analysis");
		Queue<Token> stream = new Queue<Token>();
		Lexer lexer = new Lexer();
		lexer.ParseFile(ref stream);
		ErrorHandling.Checkpoint();
		
		// If args[1] == 0 print tokens
		if (arguments.stage == 0) {
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
		ErrorHandling.Checkpoint();

		// If args[1] == 1 print syntax ast tree
		if (arguments.stage == 1) {
			AST.PrintInfo(" ");
			return;
		}

		// Semantic analysis
		SymbolTable.InitializeSymbolTable();
		ErrorHandling.ChangeStage("Semantic analysis");
		AST.Verify();
		ErrorHandling.Checkpoint();

		// If args[1] == 2 print semantic ast tree
		if (arguments.stage == 2) {
			AST.PrintInfo("");
			return;
		}
		
		
		// Code generation
		ErrorHandling.ChangeStage("Code generation");
		string outputFileName = arguments.outputFile;
		var codeGen = new CodeGen.CodeGenContext("CompiledProgram");
		AST.Generate(codeGen);
		codeGen.Save(outputFileName);
		ErrorHandling.Checkpoint();

		Console.WriteLine($"Code generation successful. Output: {outputFileName}");
		var asmPath = Path.GetFullPath(outputFileName);
		var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(asmPath);
		var programType = asm.GetType("Program");
		var mainMethod = programType.GetMethod("_Main",
			BindingFlags.Public | BindingFlags.Static);
		
		Console.WriteLine($"Running Program.Main from {outputFileName}:");
		mainMethod.Invoke(null, null);
	}
}
