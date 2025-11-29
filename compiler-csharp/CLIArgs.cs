using System.CommandLine;
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
			Description = "Name of resulting file",
			DefaultValueFactory = result => "main.dll"
		};
		oFile.HelpName = "filepath";

		Option<int> stage = new("-s"){
			Description = "Stage to stop after (0-lexic, 1-syntax, 2-semantic, 3-codegen)",
			DefaultValueFactory = result => {
				if(result.Tokens.Count == 0) return 3;
				if(int.TryParse(result.Tokens.Single().Value, out int stage)) return stage;
				return 3;
			}
		};
		stage.HelpName = "stage number";

		RootCommand command = new("Skebob language compiler.");
		command.Arguments.Add(iFile);
		command.Options.Add(oFile);
		command.Options.Add(stage);

		command.SetAction(result => {
			arguments.inputFile = result.GetValue(iFile);
			arguments.outputFile = result.GetValue(oFile);
			arguments.stage = result.GetValue(stage);
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
