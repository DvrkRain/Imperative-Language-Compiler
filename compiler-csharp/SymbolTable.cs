namespace SemanticAnalyzer.SymbolTable;

public enum ScopeType {
    Global,
    Loop,
    Branch,
    Routine,
    Record
}

public abstract class Entry
{
    public string Name { get; } // Each instance has its own name
    public int used = 0;
    
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
        this.Type = type;
        this.Value = value;
    }
}

public class Routine : Entry
{
    public List<Variable> Parameters { get; } // Routine parameters contained in list to keep order
    public string ReturnType { get; } // Each routine might have a return type
    public Scope? BodyScope; // Each routine might have a body scope

    public bool HasBody = false;
    
    public Routine(string name, List<Variable>? parameters = null, string returnType = "void") : base(name)
    {
        Parameters = parameters ?? new List<Variable>();
        ReturnType = returnType;
        BodyScope = null;
    }
}

public class Type : Entry
{
    public string BaseType { get; } // Each type has a base type (integer, real, boolean, array, record), type == baseType -> baseType in builtInTypes
    public Scope? TypeScope; // Each type might have its scope
    
    public Type(string name, string baseType, Scope? scope = null) : base(name)
    {
		this.TypeScope = scope;
        BaseType = baseType;
    }
}


public class Scope
{
    private Dictionary<string, Entry> _entries;
    
    public ScopeType? scopeType { get; }

    public Scope? Parent;
    
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

    public Entry? LookupLocalEntry(string name) {
        return _entries.TryGetValue(name, out Entry entry) ? entry : null;
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

public static class SymbolTable
{
    private static Scope _globalScope;
    private static Scope _currentScope;
    
    public static void InitializeSymbolTable()
    {
        _globalScope = new Scope(null, ScopeType.Global);
        _currentScope = _globalScope;
        
        // Инициализация встроенных типов
        InitializePrimitiveTypes();
    }
    
    private static void InitializePrimitiveTypes() {
        Type integer = new Type("integer", "integer");
        Type real = new Type("real", "real");
        Type boolean = new Type("boolean", "boolean");

        integer.used = 1;
        real.used = 1;
        boolean.used = 1;
        
        _globalScope.AddEntry(integer);
        _globalScope.AddEntry(real);
        _globalScope.AddEntry(boolean);
        
    }
    
    public static void EnterScope(ScopeType scopeType)
    {
        _currentScope = new Scope(_currentScope, scopeType);
    }
    
    public static void ExitScope()
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
    
    public static Scope GetCurrentScope()
    {
        return _currentScope;
    }
    

    public static Scope GetGlobalScope()
    {
        return _globalScope;
    }
    

    public static bool DeclareEntry(Entry entry)
    {
        return _currentScope.AddEntry(entry);
    }
    
    public static Entry? FindEntry(string name, bool local = false) {
        if (local) return _currentScope.LookupLocalEntry(name);
        return _currentScope.LookupEntry(name);
    }

    public static bool IsInsideType(ScopeType scopeType, bool current = false) {
        if (current) return _currentScope.scopeType == scopeType;
        return _currentScope.IsInsideType(scopeType);
    }

    public static bool UseEntry(string identifier) {
        Entry? entry = FindEntry(identifier);

        if (entry is null) return false;

        entry.used += 1;
        return true;
    }

    public static bool UnuseEntry(string identifier) {
        Entry? entry = FindEntry(identifier);

        if (entry is null || entry.used == 0) return false;
        
        entry.used -= 1;
        return true;
    }

    public static bool IsUsed(string identifier) {
        Entry? entry = FindEntry(identifier);
        if (entry is null || entry.used == 0) return false;
        return true;
    }
}
