using System.CommandLine;
using Compiler.Data;

namespace Compiler;
public struct Arguments {
	public string inputFile;
	public string outputFile;
	public int stage;
	public bool valid;

	public override string ToString() => $"input file: {this.inputFile}, output file: {this.outputFile}, stage code: {this.stage}.";
}


public static class CLIParser {
	public static Arguments ParseArgs(string[] args) {
		Arguments arguments = new();
		arguments.valid = false;

		Argument<string> iFile = new("filepath"){
			Description = "Name of source code file"
		};

		Option<string> oFile = new("-o"){
			HelpName = "filepath",
			Description = "Name of resulting file",
			DefaultValueFactory = result => "main.dll",
		};
		

		Option<int> stage = new("-s"){
			HelpName = "stage number",
			Description = "Stage to stop after (0-lexic, 1-syntax, 2-semantic, 3-codegen)",
			DefaultValueFactory = result => {
				if(result.Tokens.Count == 0) return 3;
				if(int.TryParse(result.Tokens.Single().Value, out int stage)) return stage;
				return 3;
			},
		};

		Option<bool> lex = new("--lex"){
			Description = "Stop after lexical analysis and print token queue",
			DefaultValueFactory = result => false
		};

		Option<bool> syn = new("--syn"){
			Description = "Stop after syntax analysis and print program AST",
			DefaultValueFactory = result => false
		};

		Option<bool> sem = new("--sem"){
			Description = "Stop after semantic analysis and print optimized program AST",
			DefaultValueFactory = result => false
		};

		Option<bool> gen = new("--gen"){
			Description = "Stop after code generation and do not run executable",
			DefaultValueFactory = result => false
		};

		RootCommand command = new("Skebob language compiler.");
		command.Arguments.Add(iFile);
		command.Options.Add(oFile);
		command.Options.Add(stage);
		command.Options.Add(lex);
		command.Options.Add(syn);
		command.Options.Add(sem);
		command.Options.Add(gen);

		command.SetAction(result => {
			arguments.stage = result.GetValue(stage);
			if(result.GetValue(lex)) arguments.stage = 0;
			else if(result.GetValue(syn)) arguments.stage = 1;
			else if(result.GetValue(sem)) arguments.stage = 2;
			else if(result.GetValue(gen)) arguments.stage = 3;

			string? name = result.GetValue(iFile);
			if(name != null)
				arguments.inputFile = name;

			name = result.GetValue(oFile);
			if(name != null)
				arguments.outputFile = name;

			FileReader.SetFile(arguments.inputFile);
			arguments.valid = true;
		});
		ParseResult res = command.Parse(args);

		if(res.Errors.Count != 0) {
			foreach(var err in res.Errors)
				Console.Error.WriteLine(err.Message);
			Environment.Exit(1);
		}
		res.Invoke();
		return arguments;
	}
}
