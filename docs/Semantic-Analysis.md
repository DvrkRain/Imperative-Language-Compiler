# Semantic Analysis

## Overview

Semantic analysis is performed through recursive validation of the AST built during syntax analysis. After the `Parse()` phase completes, the `Verify()` method is invoked on the root `ProgramNode`, triggering a depth-first traversal that validates semantics, checks types, manages symbol tables, and optimizes the tree.

## Entry Point

Semantic analysis is initialized in `Program.Main()` after syntax analysis:

```csharp
SymbolTable.InitializeSymbolTable();
ErrorHandling.ChangeStage("Semantic analysis");
AST.Verify();
```

## Symbol Table Architecture

### Entry Hierarchy

The symbol table stores three types of program entities, all inheriting from the abstract `Entry` class:

**Entry Base Class:**
- `Name` - unique identifier within scope
- `used` - reference counter for optimization hints

**Variable Entry:**
```csharp
public class Variable : Entry {
    public string Type { get; }
    public object? Value { get; set; }
}
```
- Stores variable type and optional compile-time constant value
- `Value` used for constant propagation during optimization

**Routine Entry:**
```csharp
public class Routine : Entry {
    public List<Variable> Parameters { get; }
    public string ReturnType { get; }
    public bool HasBody { get; set; }
}
```
- Maintains ordered parameter list
- `HasBody` flag supports forward declarations
- Parameters stored as `Variable` objects to preserve order

**Type Entry:**
```csharp
public class Type : Entry {
    public string BaseType { get; }
    public Scope? TypeScope { get; set; }
}
```
- `BaseType` resolves through alias chain to primitive: `integer`, `real`, `boolean`, `array`, or `record`
- `TypeScope` stores record fields or array metadata for structured types

### Scope Structure

Scopes form a tree hierarchy with parent links for nested name resolution:

```csharp
public class Scope {
    private Dictionary<string, Entry> _entries;
    public ScopeType? scopeType { get; }
    public Scope? Parent { get; set; }
}
```

**Scope Types:**
- `ScopeType.Global` - top-level declarations
- `ScopeType.Routine` - function bodies with parameters
- `ScopeType.Loop` - loop bodies (for iterator variables)
- `ScopeType.Record` - record type field definitions

### Lookup Operations

**Local Lookup:**
```csharp
public Entry? LookupLocalEntry(string name)
```
- Searches only current scope
- Used for redeclaration checks
- Returns `null` if not found

**Hierarchical Lookup:**
```csharp
public Entry? LookupEntry(string name)
```
- Searches current scope, then recursively walks parent chain
- Implements lexical scoping with shadowing support
- Returns first match found, or `null` if not in any scope

**Example Scope Resolution:**
```
Global Scope: { x: integer }
  └─ Routine Scope: { x: real, y: boolean }
       └─ Loop Scope: { i: integer }
```
- Lookup `i` in Loop → finds `i: integer` (local)
- Lookup `x` in Loop → finds `x: real` (shadows global)
- Lookup `y` in Loop → finds `y: boolean` (parent scope)

### Global Symbol Table Interface

Static class providing unified access to scope management:

**Initialization:**
```csharp
public static void InitializeSymbolTable()
```
- Resets global and current scope pointers
- Calls `InitializePrimitiveTypes()` to register `integer`, `real`, `boolean`
- Marks primitive types as used to prevent removal

**Scope Management:**
```csharp
public static void EnterScope(ScopeType scopeType)
```
- Creates new scope with current as parent
- Updates `_currentScope` pointer

```csharp
public static void ExitScope()
```
- Restores parent scope
- Throws exception if attempting to exit global scope

**Entry Operations:**
```csharp
public static bool DeclareEntry(Entry entry)
```
- Adds entry to current scope
- Returns `false` if name already exists locally

```csharp
public static Entry? FindEntry(string name, bool local = false)
```
- If `local = true`: searches only current scope
- If `local = false`: searches scope hierarchy
- Returns matched entry or `null`

**Context Queries:**
```csharp
public static bool IsInsideType(ScopeType scopeType, bool current = false)
```
- If `current = true`: checks only current scope type
- If `current = false`: checks if any ancestor scope matches type
- Used for validating context-sensitive statements (break in loop, return in function)

**Usage Tracking:**
```csharp
public static bool UseEntry(string identifier)
public static bool UnuseEntry(string identifier)
public static bool IsUsed(string identifier)
```
- Tracks reference counts for unused variable warnings
- Supports future optimization passes

## Core Verification Mechanisms

### Recursive Verification

The base `Node` class provides default `Verify()` implementation that recursively validates all children:

```csharp
public virtual void Verify() {
    foreach(var child in childs)
        child.Verify();
}
```

Each node type overrides this method to add specific semantic checks before or after child validation.

### Type System Integration

Types are resolved and validated through the symbol table:

**Built-in Types:**
- `integer`, `real`, `boolean` - registered during initialization
- Type checking via `DedicatedWords.BuiltIn(type)`

**User-Defined Types:**
- Records - structures with named fields stored in `TypeScope`
- Arrays - homogeneous collections with size metadata in `TypeScope`
- Type aliases - resolved through `BaseType` chain to primitives

## Semantic Checks by Node Type

### Variable Declaration (VarNode)

Performs multiple validations:

**Uniqueness Check:**
```csharp
if (SymbolTable.FindEntry(identifier.Name(), localOnly: true) != null)
    ErrorHandling.Add("Identifier already exists");
```
- Uses local lookup to prevent redeclaration in same scope
- Allows shadowing of outer scope variables

**Type Inference:**
- If explicit type omitted: `var x is 5;`
- Infers type from initializer expression
- Updates `_type` from child `ExpressionNode.Type()`

**Type Resolution:**
- Resolves type aliases to base types
- Validates type exists: `SymbolTable.FindEntry(this._type) is Type`
- Follows alias chain until reaching primitive type

**Symbol Registration:**
```csharp
SymbolTable.DeclareEntry(new Variable(identifier.Name(), this._type, val));
```
- Adds variable to current scope with name, type, and optional constant value
- Constant values enable compile-time optimization

### Type Declaration (TypeNode)

Manages the type system:

**Redeclaration Check:**
```csharp
if (SymbolTable.FindEntry(identifier.value, localOnly: true) != null)
    ErrorHandling.Add("Type redeclaration");
```
- Ensures type name is unique in current scope

**Type Category Processing:**

1. **Type Aliases:** `type MyInt is integer`
    - Resolves referenced type through symbol table
    - Follows `BaseType` chain to primitive:
      ```csharp
      while (!DedicatedWords.BuiltIn(baseType))
          baseType = SymbolTable.FindEntry(baseType).BaseType;
      ```

2. **Array Types:** `type Arr is array [10] integer`
    - Validates array size expression is integer type
    - Creates synthetic `TypeScope` with metadata fields:
      ```csharp
      typeScope.AddEntry(new Variable("size", "integer"));
      typeScope.AddEntry(new Variable("type", "void", elementType));
      ```
    - Enables `.size` property access at runtime

3. **Record Types:** `type Person is record ... end`
    - Enters new `ScopeType.Record` scope
    - Validates all field declarations as `VarNode`
    - Captures completed scope as `TypeScope`:
      ```csharp
      SymbolTable.EnterScope(ScopeType.Record);
      this.childs[1].Verify();  // Verify all fields
      typeScope = SymbolTable.GetCurrentScope();
      SymbolTable.ExitScope();
      typeScope.Parent = null;  // Detach from hierarchy
      ```

**Type Registration:**
```csharp
Type newType = new Type(identifier.value, baseType, typeScope);
SymbolTable.DeclareEntry(newType);
```

### Routine Declaration (RoutineNode)

Complex validation with forward declaration support:

**Forward Declaration Matching:**
```csharp
switch(SymbolTable.FindEntry(identifier)) {
    case Routine routine when routine.HasBody:
        ErrorHandling.Add("Routine redeclaration");
        break;
    case Routine routine:
        // Validate signatures match
        routine.HasBody = true;
        this.implementation = true;
        break;
    case null:
        // First declaration
        Routine newRoutine = new Routine(identifier, parameters, returnType);
        newRoutine.HasBody = this.has_body;
        SymbolTable.DeclareEntry(newRoutine);
        break;
}
```

**Signature Validation:**
- Return type must match: `routine.ReturnType == this._type`
- Parameter count must match: `routine.Parameters.Count == param_number`
- Each parameter name and type must match exactly

**Parameter Validation:**
```csharp
SymbolTable.EnterScope(ScopeType.Routine);
for (int i = 1; i < 1 + param_number; i++) {
    ParameterNode param = this.childs[i];
    SymbolTable.DeclareEntry(parameters[i-1]);
}
```
- Ensures parameter names are unique using `HashSet`
- Registers parameters as variables in routine scope

**Return Analysis:**
```csharp
Returning.Push(new ReturningStatus(false, this._type));
this.childs.Last().Verify();  // Verify body
ReturningStatus stat = Returning.Pop();
if (this._type != "void" && !stat.returned)
    ErrorHandling.Add("Return not guaranteed");
```
- Uses global `Returning` stack to track control flow
- Non-void functions must guarantee return on all paths

**Scope Detachment:**
```csharp
Scope curScope = SymbolTable.GetCurrentScope();
SymbolTable.ExitScope();
curScope.Parent = null;
```
- Prevents routine scope from leaking into outer context

### Expression Nodes (ExpressionNode & OperationNode)

Perform compile-time optimization through constant folding:

**Constant Folding (OperationNode):**

When all operands are compile-time constants (`PrimaryNode` with constant values), operations are evaluated immediately:

```csharp
if (all childs are PrimaryNode with constant values) {
    switch(this._operation) {
        case "+":
            compute result
            this.childs[0] = new PrimaryNode(position, result, true);
            this.arg_number = 0;  // Mark as leaf
            break;
    }
}
```

**Supported Optimizations:**
- **Arithmetic**: `+`, `-`, `*`, `/`, `%` for integer/real
- **Relations**: `<`, `<=`, `>`, `>=`, `==`, `/=`
- **Logic**: `and`, `or`, `xor`, `not` for boolean
- **Dot operator**:
    - Combines integers into real: `3 . 14` → `3.14`
    - Resolves record field access through `TypeScope`

**Type Inference:**
```csharp
public override void Verify() {
    base.Verify();  // Verify children first
    switch(this.childs[0]) {
        case PrimaryNode prime:
            this._type = prime.Type();
            break;
        case OperationNode oper:
            this._type = oper.Type();
            this.childs[0] = oper.Value();  // Collapse tree
            break;
    }
}
```

### Primary Node (PrimaryNode)

Performs constant propagation and variable resolution:

**Variable Resolution:**
```csharp
case string id:
    Variable vr = SymbolTable.FindEntry(id);
    this._type = vr.Type;
    if (vr.Value != null && vr.Value is not Scope)
        this.value = vr.Value;  // Propagate constant
```

**Type Inference:**
- `int` literal → `_type = "integer"`
- `float` literal → `_type = "real"`
- `bool` literal → `_type = "boolean"`
- Variable identifier → inherits type from symbol table entry

**Loop Context Handling:**
```csharp
if (SymbolTable.IsInsideType(ScopeType.Loop)) return;
```
- Skips value propagation inside loops (values may change)

### Control Flow Nodes

**IfNode**:
- Validates condition is boolean: `childs[0].Type() == "boolean"`
- Manages return tracking for both branches:
  ```csharp
  Returning.Push(ReturningStatus.Copy(Returning.Peek()));
  childs[1].Verify();  // then-branch
  bool thenReturns = Returning.Pop().returned;
  
  Returning.Push(ReturningStatus.Copy(Returning.Peek()));
  childs[2].Verify();  // else-branch
  bool elseReturns = Returning.Pop().returned;
  
  if (thenReturns && elseReturns)
      Returning.Peek().returned = true;
  ```

**ForNode**:
- Enters `ScopeType.Loop` scope
- Declares iterator variable:
  ```csharp
  SymbolTable.DeclareEntry(new Variable(iteratorName, "integer"));
  ```
- Validates range expressions are integer type
- Handles `reverse` by swapping boundaries: `(childs[1], childs[2]) = (childs[2], childs[1])`

**WhileNode**:
- Enters `ScopeType.Loop` scope
- Validates condition is boolean type
- Manages return status tracking through nested scope

### Statement Nodes

**AssignmentNode**:

**Type Compatibility Check:**
Built-in types use conversion table:
- `integer ← real`: `(int)Math.Round(val)`
- `integer ← boolean`: `true → 1`, `false → 0`
- `real ← integer`: direct conversion
- `real ← boolean`: `true → 1.0`, `false → 0.0`
- `boolean ← integer`: `1 → true`, `0 → false`, else error
- `boolean ← real`: error

User-defined types require exact name match:
```csharp
if (flag && this._type != this.childs[1].Type())
    ErrorHandling.Add("Non-built-in types cannot be casted");
```

**Compile-Time Evaluation:**
```csharp
if (DedicatedWords.BuiltInStrict(this._type) && 
    this.childs[0].GetChilds()[0] is PrimaryNode target &&
    SymbolTable.FindEntry(target.Name()) is Variable var) {
    var.Value = this.cast(var.Type, expressionValue);
}
```
- Updates variable's stored value in symbol table
- Enables constant propagation to subsequent uses

**ReturnNode**:
```csharp
if (!SymbolTable.IsInsideType(ScopeType.Routine))
    ErrorHandling.ReturnOutsideFunction();

ReturningStatus stat = Returning.Pop();
if (stat.ret_type != this.childs[0].Type())
    ErrorHandling.Add("Return type mismatch");
stat.returned = true;
Returning.Push(stat);
```

**BreakNode & ContinueNode**:
```csharp
if (!SymbolTable.IsInsideType(ScopeType.Loop))
    ErrorHandling.Add("Used outside loop");
```

### Field Access (FieldAccessNode)

Resolves field and array access chains:

**Resolution Process:**
```csharp
// 1. Resolve base variable
Variable var = SymbolTable.FindEntry(baseName);
this._type = var.Type;

// 2. Resolve type scope
Type type = SymbolTable.FindEntry(this._type);
Scope typeScope = type.TypeScope;

// 3. For each access in chain
for (int i = 1; i < childs.Count; i++) {
    if (childs[i] is PrimaryNode field) {
        // Dot access: lookup field in type scope
        Variable fieldVar = typeScope.LookupEntry(field.Name());
        this._type = fieldVar.Type;
    } else if (childs[i] is ExpressionNode index) {
        // Array access: validate index is integer
        if (index.Type() != "integer")
            ErrorHandling.Add("Array index must be integer");
    }
}
```

**Special Cases:**
- Array `.size` property → returns `integer` type
- Record fields → validates field exists in `TypeScope`

### Parameter Node (ParameterNode)

Handles function parameters with special array support:

**Sizeless Array Parameters:**
```csharp
case ArrayNode arrayNode:
    this._type = $"_array{arrayNode.Type()}";
    Scope arrayScope = new Scope();
    arrayScope.AddEntry(new Variable("size", "integer"));
    arrayScope.AddEntry(new Variable("type", "void", arrayNode.Type()));
    Type newArrayType = new Type(this._type, "array");
    newArrayType.TypeScope = arrayScope;
    SymbolTable.DeclareEntry(newArrayType);
```
- Creates synthetic type for each `array [] Type` parameter
- Naming convention: `_arrayType` (e.g., `_arrayinteger`)
- Enables runtime size access through `.size` property

## AST Transformations

### Constant Folding Example

**Before Verify:**
```
ExpressionNode (type: void)
├── PrimaryNode(5)
├── PrimaryNode(3)
└── OperationNode("+", arg_number: 2)
```

**After Verify:**
```
ExpressionNode (type: integer)
└── PrimaryNode(8)
```

The operation is evaluated at compile-time, reducing tree depth and eliminating runtime computation.

### Dead Code Elimination

**ProgramNode** removes unreachable code after control flow changes:

```csharp
int lastIndex = this.childs.Count() - 1;
for (int i = 0; i <= lastIndex; i++) {
    switch (this.childs[i]) {
        case ReturnNode when SymbolTable.IsInsideType(ScopeType.Routine):
            lastIndex = i;
            break;
        case BreakNode or ContinueNode when SymbolTable.IsInsideType(ScopeType.Loop):
            lastIndex = i;
            break;
    }
}
this.childs.RemoveRange(lastIndex + 1, this.childs.Count() - (lastIndex + 1));
```

**Example:**
```
Before:                After:
return x;              return x;
print 5;               (removed)
y := 10;               (removed)
```

### Expression Simplification

**ExpressionNode** collapses single-value expressions:
```csharp
if (this.childs[0] is PrimaryNode)
    return this.childs[0];  // Expression is just a value
return this;  // Keep as expression node
```

## Key Features

### Static Type Checking

All type mismatches detected before code generation:
- Assignment compatibility validation with built-in conversion rules
- Function return type verification across all control paths
- Operator type requirements (boolean for conditions, integer for array indices)

### Compile-Time Optimization

Significant optimizations performed during semantic analysis:
- **Constant folding**: Arithmetic/logic operations on constants evaluated at compile-time
- **Constant propagation**: Variables with known constant values replaced inline
- **Dead code elimination**: Unreachable statements after return/break/continue removed
- **Expression simplification**: Single-value expressions collapsed to reduce tree depth

### Scope Safety

Multi-level validation ensures proper scoping:
- Variables must be declared before use via hierarchical lookup
- Break/continue validated to be inside loops only
- Return validated to be inside functions only
- Nested scope shadowing fully supported through parent chain

### Forward Declaration Support

Routines can be declared without implementation, then defined later:
- **First declaration**: Registers signature with `HasBody = false`
- **Implementation**: Validates matching signature and sets `HasBody = true`
- Enables mutual recursion and flexible declaration order

## Error Detection

Comprehensive error checking across all semantic rules:
- Undeclared variables/types via symbol table lookup failures
- Type mismatches in assignments, returns, and operations
- Redeclarations caught by local scope lookups
- Invalid control flow usage through scope type validation
- Missing return statements detected via `ReturningStatus` tracking
- Parameter signature mismatches in forward declarations

All errors reported with precise source position via `Node.position` for debugging.