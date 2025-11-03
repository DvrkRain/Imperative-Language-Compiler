using System.Reflection.Metadata;

namespace SemanticAnalyzer.SymbolTable;

public enum ScopeType {
    Global,
    Loop,
    Branch,
    FunctionDeclaration,
    FunctionBody
}

public abstract class Entry
{
    public string Name { get; } // Each instance has its own name
    
    protected Entry(string name)
    {
        Name = name;
    }
}

public class Variable : Entry
{
    public string Type { get; } // Each variable has type
    public object? Value { get; } // Each variable might have value
    
    public Variable(string name, string type, object? value = null) : base(name)
    {
        Type = type;
        Value = value;
    }
}

public class Routine : Entry
{
    public List<Variable> Parameters { get; } // Routine parameters contained in list to keep order
    public string? ReturnType { get; } // Each routine might have a return type
    public Scope? BodyScope { get; } // Each routine migh have a body scope
    
    public Routine(string name, List<Variable>? parameters = null, string? returnType = null) : base(name)
    {
        Parameters = parameters ?? new List<Variable>();
        ReturnType = returnType;
        BodyScope = null;
    }
}

public class Type : Entry
{
    public string BaseType { get; } // Each type has a base type (integer, real, boolean, array, record), type == baseType -> baseType in builtInTypes
    public Scope? RecordScope { get; } // Each type might have its scope
    
    public Type(string name, string baseType) : base(name)
    {
        BaseType = baseType;
    }
}


public class Scope
{
    private Dictionary<string, Entry> _entries;
    
    public ScopeType? scopeType { get; }
    
    public Scope? Parent { get; }
    
    public Scope(Scope? parent = null, ScopeType? scopeType = null)
    {
        _entries = new Dictionary<string, Entry>();
        this.scopeType = scopeType;
        Parent = parent;
    }

    public bool AddEntry(Entry entry)
    {
        if (_entries.ContainsKey(entry.Name))
        {
            return false;
        }
        _entries[entry.Name] = entry;
        return true;
    }
    
    public Entry? LookupEntry(string name)
    {
        if (_entries.TryGetValue(name, out var entry))
        {
            return entry;
        }
        
        return Parent?.LookupEntry(name);
    }

    public bool IsInsideType(ScopeType scopeType) {
        if (this.scopeType == scopeType) return true;

        if (Parent != null) return Parent.IsInsideType(scopeType);

        return false;
    }
}

public class SymbolTable
{
    private Scope _globalScope;
    private Scope _currentScope;
    
    public SymbolTable()
    {
        _globalScope = new Scope();
        _currentScope = _globalScope;
        
        // Инициализация встроенных типов
        InitializePrimitiveTypes();
    }
    
    private void InitializePrimitiveTypes()
    {
        _globalScope.AddEntry(new Type("integer", "integer"));
        _globalScope.AddEntry(new Type("real", "real"));
        _globalScope.AddEntry(new Type("boolean", "boolean"));
    }
    
    public void EnterScope()
    {
        _currentScope = new Scope(_currentScope);
    }
    
    public void ExitScope()
    {
        if (_currentScope.Parent != null)
        {            
            _currentScope = _currentScope.Parent;
        }
        else
        {
            throw new InvalidOperationException("Cannot exit global scope");
        }
    }
    
    public Scope GetCurrentScope()
    {
        return _currentScope;
    }
    

    public Scope GetGlobalScope()
    {
        return _globalScope;
    }
    

    public bool DeclareEntry(Entry entry)
    {
        return _currentScope.AddEntry(entry);
    }
    
    public Entry? FindEntry(string name)
    {
        return _currentScope.LookupEntry(name);
    }

    public bool IsInsideType(ScopeType scopeType) {
        return _currentScope.IsInsideType(scopeType);
    }
}
