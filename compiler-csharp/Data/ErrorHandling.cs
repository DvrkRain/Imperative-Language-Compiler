namespace Data.ErrorHandling;

public static class ErrorHandling {
    private static List<string> _errorList = new();

    public static void Add(string message) {
        _errorList.Add(message);
    }
    
    public static int Count() {
        return _errorList.Count;
    }

    public static void Clear() {
        _errorList.Clear();
    }
    
    public static void PrintErrors() {
        foreach (string error in _errorList) {
            Console.WriteLine(error);
        }
    }
}