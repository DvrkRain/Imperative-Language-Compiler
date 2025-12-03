# Code Generation

## Overview

Code generation is the final compilation phase that transforms the validated AST into executable .NET IL (Intermediate Language) bytecode. After semantic analysis completes, the `Generate()` method is invoked on the root `ProgramNode`, triggering a recursive traversal that emits IL instructions into a dynamically created assembly. The output is a `.dll` file containing executable .NET code.

## Entry Point

Code generation is initialized in `Program.Main()` after semantic analysis:

```csharp
ErrorHandling.ChangeStage("Code generation");
string outputFileName = arguments.outputFile;
var codeGen = new CodeGen.CodeGenContext("CompiledProgram");
AST.Generate(codeGen);
codeGen.Save(outputFileName);
```

The generated assembly is immediately loadable and executable:
```csharp
var asm = AssemblyLoadContext.Default.LoadFromAssemblyPath(outputFileName);
var programType = asm.GetType("Program");
var mainMethod = programType.GetMethod("_Main", BindingFlags.Public | BindingFlags.Static);
mainMethod.Invoke(null, null);
```

## CodeGenContext Architecture

### Purpose

`CodeGenContext` maintains all state required for IL emission during the recursive AST traversal. It manages assembly construction, type resolution, symbol mappings, and control flow context.

### Core Components

**Assembly Infrastructure:**
```csharp
public PersistedAssemblyBuilder AssemblyBuilder { get; }
public ModuleBuilder ModuleBuilder { get; }
public TypeBuilder ProgramTypeBuilder { get; }
```
- Uses .NET 9's `PersistedAssemblyBuilder` for disk persistence
- Creates single module "MainModule"
- Defines public class "Program" to contain all generated code

**IL Emission Context:**
```csharp
public ILGenerator CurrentIL { get; set; }
public MethodBuilder CurrentMethod { get; set; }
```
- `CurrentIL` - active instruction stream for current method
- `CurrentMethod` - metadata builder for current method
- Updated when entering/exiting routine bodies

**Symbol Mappings:**
```csharp
public Dictionary<string, LocalBuilder> LocalVariables { get; }
public Dictionary<string, FieldInfo> GlobalFields { get; }
public Dictionary<string, MethodInfo> Methods { get; }
public Dictionary<string, TypeBuilder> UserTypes { get; }
public Dictionary<string, System.Type> ArrayTypes { get; }
```

- `LocalVariables` - maps variable names to `LocalBuilder` instances for current method
- `GlobalFields` - maps global variable names to static `FieldInfo`
- `Methods` - maps routine names to `MethodInfo` for call resolution
- `UserTypes` - maps record type names to `TypeBuilder` instances
- `ArrayTypes` - maps array type names to element types

**Parameter Tracking:**
```csharp
public Dictionary<string, int> ParameterIndices { get; }
public Dictionary<string, System.Type> ParameterTypes { get; }
```
- Maps parameter names to argument indices (0, 1, 2, ...)
- Stores parameter types for validation
- Cleared when exiting routine context

**Control Flow Management:**
```csharp
public Stack<LoopContext> LoopStack { get; }

public class LoopContext {
    public Label BreakLabel { get; }
    public Label ContinueLabel { get; }
}
```
- Maintains stack of active loops for `break`/`continue` resolution
- Each context stores labels for loop exit and continuation points

### Type Resolution

**ResolveType(string typeName):**
```csharp
public System.Type ResolveType(string typeName) {
    if (ArrayTypes.ContainsKey(typeName))
        return ArrayTypes[typeName].MakeArrayType();
    if (UserTypes.ContainsKey(typeName))
        return UserTypes[typeName];
    if (typeMap.ContainsKey(typeName))
        return typeMap[typeName];
    return typeof(object);
}
```

**Type Mapping:**
- Built-in types: `integer → int`, `real → float`, `boolean → bool`, `void → void`
- User types: records and arrays registered during type declaration processing
- Type aliases: registered via `RegisterTypeAlias()`

### Registration Methods

**RegisterTypeAlias(string aliasName, System.Type baseType):**
- Adds type alias to `typeMap` for resolution
- Called by `TypeNode` for simple type aliases

**RegisterUserType(string typeName, TypeBuilder typeBuilder):**
- Registers record type with `TypeBuilder`
- Adds to both `UserTypes` and `typeMap`
- Called by `TypeNode` after record class creation

**RegisterArrayType(string typeName, System.Type elementType):**
- Registers array type with element type
- Stores element type for `.MakeArrayType()` call
- Called by `TypeNode` and `ParameterNode`

### Assembly Persistence

**Save(string outputPath):**
```csharp
public void Save(string outputPath) {
    ProgramTypeBuilder.CreateType();  // Finalize Program class
    AssemblyBuilder.Save(outputPath);  // Write to disk
}
```

## ILHelper Utilities

Static helper class for type-safe constant loading with automatic conversions:

**Integer Loading:**
- `EmitLoadInt(ILGenerator, int)` - Direct integer load
- `EmitLoadInt(ILGenerator, float)` - Rounds float to nearest integer
- `EmitLoadInt(ILGenerator, bool)` - Converts true→1, false→0

**Real (Float) Loading:**
- `EmitLoadReal(ILGenerator, int)` - Converts integer to float
- `EmitLoadReal(ILGenerator, float)` - Direct float load
- `EmitLoadReal(ILGenerator, bool)` - Converts true→1.0, false→0.0

**Boolean Loading:**
- `EmitLoadBool(ILGenerator, bool)` - Loads boolean as 0 or 1
- `EmitLoadBool(ILGenerator, int)` - Validates 0→false, 1→true, else throws
- `EmitLoadBool(ILGenerator, float)` - Throws exception (invalid conversion)

All methods emit appropriate `Ldc_*` (Load Constant) opcodes with proper type conversions.

## Code Generation Strategy

### Recursive Traversal

The base `Node` class provides default recursive implementation:

```csharp
public virtual void Generate(CodeGenContext context) {
    foreach(var child in childs)
        child.Generate(context);
}
```

Each node type overrides this to emit specific IL instructions at appropriate points during traversal.

### Generation Patterns by Category

**Declaration Nodes:**
- **Variable declarations** distinguish between global (static fields) and local (method locals) based on current method context
- **Type declarations** create .NET type definitions: classes for records, metadata for arrays
- **Routine declarations** define methods with proper signatures, save/restore context for nested scopes

**Expression Nodes:**
- **Primary nodes** load constants onto evaluation stack using `ILHelper`, or load variables via `Ldloc`/`Ldsfld`/`Ldarg`
- **Operation nodes** first generate child expressions (pushing operands), then emit operator instructions (`Add`, `Mul`, `Ceq`, etc.)
- **Field access nodes** generate address loading sequences for lvalues, handling chains of field/array access

**Statement Nodes:**
- **Assignment** generates rvalue expression then stores via `Stloc`/`Stsfld`/`Stfld`/`Stelem`
- **Control flow** (if/while/for) uses IL labels for branching and looping
- **Break/continue** emit unconditional jumps to labels from loop context stack
- **Return** generates return expression then emits `Ret` instruction

**Built-in Functions:**
- **Print** generates expression values then calls appropriate `Console.WriteLine` overload via reflection

### Stack-Based Evaluation Model

All code generation follows IL's stack-based execution model:

1. **Operand Loading**: Push values onto evaluation stack
    - Constants: `Ldc_I4`, `Ldc_R4` opcodes
    - Variables: `Ldloc`, `Ldsfld`, `Ldarg` opcodes
    - Fields/arrays: `Ldfld`, `Ldelem` opcodes

2. **Operation Execution**: Operators consume stack values, push results
    - Binary operators: pop two values, push result
    - Unary operators: pop one value, push result
    - Comparisons: pop operands, push boolean (0 or 1)

3. **Value Storage**: Store stack top to memory
    - Variables: `Stloc`, `Stsfld` opcodes
    - Fields: `Stfld` opcode
    - Arrays: `Stelem` opcode

### Control Flow with Labels

Structured control flow implemented through IL label system:

**Conditional Branching:**
- Generate condition expression (pushes boolean)
- `Brfalse` label - jump if false
- `Brtrue` label - jump if true
- `MarkLabel` - target for jumps

**Loops:**
- Start label marks loop beginning
- Condition check with conditional branch to end label
- Body execution
- Unconditional branch (`Br`) back to start label
- End label marks loop exit

**Break/Continue:**
- Loop context provides break and continue labels
- Break emits `Br` to end label
- Continue emits `Br` to start (while) or increment (for) label

### Context Management

**Global vs Local Scope:**
- Check `CurrentMethod.Name == "_Main"` to distinguish global declarations
- Globals stored as static fields on `ProgramTypeBuilder`
- Locals stored in method's `ILGenerator.DeclareLocal()`

**Routine Context Switching:**
- Save current `ILGenerator`, `LocalVariables`, `ParameterIndices`
- Create new method's `ILGenerator`
- Generate routine body with clean context
- Restore parent context after completion

**Parameter Handling:**
- Map parameters to local variables for uniform access pattern
- Copy arguments to locals at routine entry (`Ldarg` → `Stloc`)
- Enables consistent variable access throughout generated code

### Type System Integration

**Built-in Type Mapping:**
- Source `integer` → .NET `System.Int32`
- Source `real` → .NET `System.Single` (32-bit float)
- Source `boolean` → .NET `System.Int32` (0 = false, 1 = true)

**User-Defined Types:**
- **Records**: Generate sealed classes with public fields
    - Define default constructor that initializes nested types
    - Create type with `TypeBuilder.CreateType()` before use
- **Arrays**: Use .NET array types with element type
    - Size stored in separate variable/field at declaration
    - Instantiated with `Newarr` instruction at variable declaration

**Type Aliases:**
- Resolved during `ResolveType()` lookup
- No runtime representation, purely compile-time mapping

### Optimization Techniques

**Constant Folding Impact:**
- Constants folded during semantic analysis don't generate runtime operations
- Single `PrimaryNode` with computed value instead of operation tree
- Reduces generated IL instruction count

**Array Indexing:**
- Source language uses 1-based indexing, IL requires 0-based
- Subtract 1 from index before `Ldelem`/`Stelem`:
  ```
  push index → push 1 → Sub → Ldelem
  ```

## Output Format

The compiler generates a .NET assembly with the following structure:

```
CompiledProgram.dll
└── Module: MainModule
    └── Type: Program (public class)
        ├── Static Fields: global variables
        ├── Methods: user-defined routines
        ├── Method: _Main (entry point)
        └── Nested Types: record definitions
```

**Entry Point Convention:**
- Main execution starts from `Program._Main()` static method
- No parameters, void return type
- Invoked via reflection after assembly load

**Generated Code Characteristics:**
- All global variables become static fields
- All functions become static methods
- Record types become nested sealed classes
- Arrays use standard .NET array types

## Key Features

### Context Preservation

Routine generation saves and restores context for nested method compilation:
- Separate `LocalVariables` dictionary per method
- Independent `ParameterIndices` per routine
- Restores parent method context after completing nested routine

### Stack-Based Execution

All expressions evaluated via IL evaluation stack:
- Natural composition for complex expressions
- Operators implicitly consume operands from stack
- No need for temporary variable management

### Label-Based Control Flow

Structured control flow implemented with IL labels:
- Forward branches for conditionals (`Brfalse`, `Brtrue`)
- Backward branches for loops (`Br` to start label)
- Multiple labels per construct enable complex control flow

### Immediate Execution

Generated assembly can be loaded and executed immediately:
- No separate linking step required
- Assembly contains all necessary metadata
- Compatible with .NET runtime introspection and execution