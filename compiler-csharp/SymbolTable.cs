using System.Collections;

namespace SemanticAnalyzer.SymbolTable;

public abstract class Entry
{
    public string Name { get; } // Each instance has its own name
    
    protected Entry(string name)
    {
        Name = name;
    }

    public abstract bool AddToScope();
}

public class Variable : Entry
{
    public string Type { get; } // Each variable has type
    public object? Value { get; } // Each variable might have value
    public bool IsInitialized { get; } // Case `var a : integer`
    
    public Variable(string name, string type, object? value = null) : base(name)
    {
        Type = type;
        Value = value;
        IsInitialized = value != null;
    }

    public override bool AddToScope() {
        throw new NotImplementedException();
    }
}

public class Routine : Entry
{
    public List<Variable> Parameters { get; } // Routine parameters contained in list to keep order
    public string? ReturnType { get; } // Each routine might have a return type
    public Hashtable? BodyScope { get; set; } // Each routine migh have a body scope
    public bool IsForward { get; set; } // 
    
    public Routine(string name, List<Variable>? parameters = null, string? returnType = null) : base(name)
    {
        Parameters = parameters ?? new List<Variable>();
        ReturnType = returnType;
        BodyScope = null;
        IsForward = false;
    }

    public override bool AddToScope() {
        throw new NotImplementedException();
    }
}

public class Type : Entry
{
    public string BaseType { get; set; } // Базовый тип (integer, real, boolean, array, record)
    public Hashtable? RecordScope { get; set; } // Для record типов
    public string? ArrayElementType { get; set; } // Для array типов
    public int? ArraySize { get; set; } // Размер массива (null если не указан)
    
    public Type(string name, string baseType) : base(name)
    {
        BaseType = baseType;
    }

    public override bool AddToScope() {
        throw new NotImplementedException();
    }
}

