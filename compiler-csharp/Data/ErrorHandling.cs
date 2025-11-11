using Data.Objects;

namespace Data.ErrorHandling;

public static class ErrorHandling {
    private static List<string> _errorList = new();

    public static void Add(string message) {
        _errorList.Add(message);
    }

    public static int Count() {
        return _errorList.Count;
    }
    
    public static void PrintErrors() {
        foreach (string error in _errorList) {
            Console.WriteLine(error);
        }
    }

    public static void UnexpectedEOF(Position pos, string invoker) {
        _errorList.Add($"{invoker}({pos.Row()},{pos.Col()}): End of file reached unexpectedly in command");
    }

    public static void ReturnOutsideFunction(Position pos, string invoker) {
        _errorList.Add($"{invoker}({pos.Row()},{pos.Col()}): Return used outside the funtcion body");
    }

    public static void ContinueOutsideCycle(Position pos, string invoker) {
        _errorList.Add($"{invoker}({pos.Row()},{pos.Col()}): Continue used outside the loop body");
    }

    public static void BreakOutsideCycle(Position pos, string invoker) {
        _errorList.Add($"{invoker}({pos.Row()},{pos.Col()}): Break used outside the loop body");
    }

    public static void UnexpectedTokenException(Position pos, string invoker) {
        _errorList.Add($"{invoker}({pos.Row()},{pos.Col()}): Unexpected token");
    }

    public static void MismatchedParenthesis(Position pos, string invoker) {
        _errorList.Add($"{invoker}({pos.Row()},{pos.Col()}): Mismatched parenthesis");
    }

}