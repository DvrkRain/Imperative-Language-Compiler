using System.Reflection;
using System.Runtime.Loader;
using System.CommandLine;
using Compiler.Data;
using Compiler.AST;

namespace Compiler;
public struct Arguments {
	public string sourceFile;
	public string assemblyFile;
	public int stage;
	public bool comOrRun;

	public override string ToString() => $"input file: {this.sourceFile}, output file: {this.assemblyFile}, stage code: {this.stage}.";
}

class Program {
	static void Main(string[] args) {
		Arguments arguments = new();

		// build
		Argument<string> iFile = new("filepath"){
			Description = "Name of source code file"
		};

		Option<string> oFile = new("-o") {
			HelpName = "filepath",
			Description = "Name of resulting file",
			DefaultValueFactory = result => "main.dll",
		};

		Option<int> stage = new("-s") {
			HelpName = "stage number",
			Description = "Stage to stop after (0-lexic, 1-syntax, 2-semantic, 3-codegen)",
			Recursive = true,
			DefaultValueFactory = result => {
				if(result.Tokens.Count == 0) return 3;
				if(int.TryParse(result.Tokens.Single().Value, out int stage)) return stage;
				return 3;
			},
		};

		Command build = new("build", "Build .dll assembly") {
			oFile,
			iFile,
			stage,
		};

		// run
		Argument<string> aFile = new("filepath"){
			Description = "Name of assembly file"
		};

		Option<string> sFile = new("--source") {
			HelpName = "filepath",
			Description = "Name of source code file",
		};

		Command run = new("run", "Run builded .dll assembly") {
			aFile,
			sFile
		};

		// general
		RootCommand command = new("Skebob language compiler.");
		command.Subcommands.Add(build);
		command.Subcommands.Add(run);

		build.SetAction(result => {
			arguments.stage = result.GetValue(stage);

			string? name = result.GetValue(oFile);
			if(name != null)
				arguments.assemblyFile = name;

			name = result.GetValue(iFile);
			if(name != null) {
				arguments.sourceFile = name;
				FileReader.SetFile(name);
			}
			
			Compile(arguments);
		});

		run.SetAction(result => {
			arguments.stage = result.GetValue(stage);

			string? name = result.GetValue(aFile);
			if(name != null)
				arguments.assemblyFile = name;

			name = result.GetValue(sFile);
			if(name != null) {
				arguments.sourceFile = name;
				FileReader.SetFile(name);
				Compile(arguments);
			}

			if(!File.Exists(arguments.assemblyFile)) {
				Console.WriteLine("If source file is not present, then --source option should be set for run command");
				Environment.Exit(1);
			}
			
			Run(arguments);
		});

		ParseResult res = command.Parse(args);

		if(res.Errors.Count != 0) {
			foreach(var err in res.Errors)
				Console.Error.WriteLine(err.Message);
			Environment.Exit(1);
		}
		res.Invoke();
	}

	static void Compile(Arguments arguments) {
		// Console.WriteLine(arguments.ToString());

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
		string outputFileName = arguments.assemblyFile;
		var codeGen = new CodeGen.CodeGenContext("CompiledProgram");
		AST.Generate(codeGen);
		codeGen.Save(outputFileName);
	}

	static void Run(Arguments arguments) {
		var asmPath = Path.GetFullPath(arguments.assemblyFile);
		var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(asmPath);
		var programType = asm.GetType("Program");
		var mainMethod = programType.GetMethod("_Main",
			BindingFlags.Public | BindingFlags.Static);
		
		mainMethod.Invoke(null, null);
	}
}
