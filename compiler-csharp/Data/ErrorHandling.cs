using Data.Objects;

namespace Data.ErrorHandling;
public static class ErrorHandling {
	private static string _stage = "Lexic analysis";
    private static List<string> _errorList = new();

	private static void Error(string invoker, Position pos, string message) =>
		_errorList.Add($"{invoker}\t{pos.ToString()}:\t{message}");

    public static void Add(string invoker, Position pos, string message) =>
        ErrorHandling.Error(invoker, pos, message);
	
	public static void ChangeStage(string stage_name) =>
		_stage = stage_name;

    public static int Count() =>
        _errorList.Count;
    
	public static void Checkpoint() {
		if(_errorList.Count != 0) {
			Console.WriteLine($"There are errors on {_stage} stage.");
			foreach (string error in _errorList) {
				Console.WriteLine(error);
			}
			Environment.Exit(2);
		}
	}

	// Specialized error messages
    public static void UnexpectedEOF(string invoker, Position pos) =>
        ErrorHandling.Error(invoker, pos, "End of file reached unexpectedly in command");

    public static void ReturnOutsideFunction(string invoker, Position pos) =>
        ErrorHandling.Error(invoker, pos, "Return used outside the funtcion body");

    public static void ContinueOutsideCycle(string invoker, Position pos) =>
        ErrorHandling.Error(invoker, pos, "Continue used outside the loop body");

    public static void BreakOutsideCycle(string invoker, Position pos) =>
        ErrorHandling.Error(invoker, pos, "Break used outside the loop body");

    public static void UnexpectedTokenException(string invoker, Position pos) =>
        ErrorHandling.Error(invoker, pos, "Unexpected token");

    public static void MismatchedParenthesis(string invoker, Position pos) =>
        ErrorHandling.Error(invoker, pos, "Mismatched parenthesis");
}
