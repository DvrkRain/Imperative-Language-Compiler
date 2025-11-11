using Data.Objects;

namespace Data.ErrorHandling;

public static class ErrorHandling {
    private static List<string> _errorList = new();
    
    public static int Count() {
        return _errorList.Count;
    }
    
    public static void PrintErrors() {
        foreach (string error in _errorList) {
            Console.WriteLine(error);
        }
    }

    public static void UnexpectedEOF(Position pos, string invoker) {
        _errorList.Add($"{invoker}: End of file reached unexpectedly in command at position {pos.Row()},{pos.Col()}");
    }

    public static void ReturnOutsideFunction(Position pos, string invoker) {
        _errorList.Add($"{invoker}: Return used outside the funtcion body at position {pos.Row()},{pos.Col()}.");
    }

    public static void ContinueOutsideCycle(Position pos, string invoker) {
        _errorList.Add($"{invoker}: Continue used outside the loop body at position {pos.Row()},{pos.Col()}.");
    }

    public static void BreakOutsideCycle(Position pos, string invoker) {
        _errorList.Add($"{invoker}: Break used outside the loop body at position {pos.Row()},{pos.Col()}.");
    }

    public static void UnexpectedTokenException(Position pos, string invoker) {
        _errorList.Add($"{invoker}: Unexpected token at {pos.Row()},{pos.Col()}.");
    }

}