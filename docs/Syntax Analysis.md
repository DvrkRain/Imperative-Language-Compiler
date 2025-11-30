# Syntax Analysis

## Overview

Syntax analysis is performed through recursive construction of an Abstract Syntax Tree (AST) from the token stream received from the lexical analyzer. The process begins with creating a root `ProgramNode`, which triggers cascading creation of child nodes, forming the tree structure of the program.

## Architecture

### Base Node Structure

All AST nodes inherit from the abstract `Node` class, which defines three key methods:

- **Parse(ref Queue<Token> tokenQueue)** - parsing tokens and building the tree
- **Verify()** - semantic validation (used in the next stage)
- **Generate(CodeGenContext ctx)** - IL code generation (final stage)

Each node stores:
- `position` - location in source code for error reporting
- `_type` - type of expression or construct
- `childs` - list of child nodes

### Entry Point

Syntax analysis is initialized in `Program.Main()`:

```csharp
ProgramNode AST = new ProgramNode(new Position(0, 0), main: true);
AST.Parse(ref stream);
```

## Parsing Process

### Token Dispatching (ProgramNode)

`ProgramNode` acts as a token dispatcher, analyzing each token type and delegating parsing to appropriate nodes:

**Declarations:**
- `TokenCode.variable_declaration` → creates `VarNode`
- `TokenCode.type_declaration` → creates `TypeNode`
- `TokenCode.routine_declaration` → creates `RoutineNode`

**Control Flow:**
- `TokenCode.if_statement` → creates `IfNode`
- `TokenCode.for_statement` → creates `ForNode`
- `TokenCode.while_statement` → creates `WhileNode`

**Statements:**
- `TokenCode.identifier` → distinguishes function calls from assignments via lookahead
    - `identifier (` → creates `ExpressionNode` for function call
    - `identifier :=` or `identifier

### Recursive Descent Parsing

Each node recursively creates its child nodes, forming a tree structure:

#### Variable Declaration (VarNode)

Parses three syntactic forms:
1. `var identifier : Type;` - type only
2. `var identifier : Type is Expression;` - type with initialization
3. `var identifier is Expression;` - type inference from expression

Creates child nodes:
- `PrimaryNode` for variable name
- `ExpressionNode` for initializing expression (optional)

#### Type Declaration (TypeNode)

Parses type declarations:
- **Aliases**: `type MyInt is integer` → creates `PrimaryNode`
- **Arrays**: `type Arr is array [10] integer` → creates `ArrayNode`
- **Records**: `type Person is record ... end` → creates `RecordNode`

`ArrayNode` creates:
- `ExpressionNode` for array size
- `PrimaryNode` for element type

`RecordNode` creates:
- Sequence of `VarNode` for structure fields

#### Routine Declaration (RoutineNode)

Parses function declarations with forward declaration support:
- `PrimaryNode` for function name
- Sequence of `ParameterNode` for parameters
- `ProgramNode` for function body (recursive parser invocation)
- `ExpressionNode` for single-line functions (`=> expression`)

#### Control Flow Nodes

**IfNode**:
- `ExpressionNode` for condition
- `ProgramNode` for then-branch
- `ProgramNode` for else-branch (optional)

**ForNode**:
- `PrimaryNode` for iterator name
- `ExpressionNode` for start value
- `ExpressionNode` for end value (optional)
- `ProgramNode` for loop body

**WhileNode**:
- `ExpressionNode` for condition
- `ProgramNode` for loop body

#### Expression Parsing (ExpressionNode)

The most complex node - uses the **Shunting-yard algorithm**:

**Phase 1: Conversion to Postfix Notation**
- Reads tokens: operands, operators, parentheses
- Uses operator stack with precedence handling via `Precedence.Order()`
- Processes function calls, tracking argument count
- Result: sequence of nodes in Reverse Polish Notation

**Phase 2: Building AST from Postfix**
- Uses stack for tree assembly
- `PrimaryNode` instances pushed as operands
- `OperationNode` instances pop required operands from stack
- Forms hierarchical expression structure

Created child nodes:
- `PrimaryNode` for literals and identifiers
- `OperationNode` for operators (+, -, *, /, ==, <, and, or, ., [ ])

#### Field Access (FieldAccessNode)

Parses chains of field and array element access:
- `identifier.field.subfield` → sequence of `PrimaryNode` for fields
- `identifier[expr]` → `ExpressionNode` for indices
- Any combinations: `array[5].field[3].subfield`

#### Assignment (AssignmentNode)

Parses assignment:
- `FieldAccessNode` for left-hand side (already created in `ProgramNode`)
- `ExpressionNode` for right-hand side

### Error Recovery

Base `Node` class provides `HandleUnexpectedToken()` method
- Registers error via `ErrorHandling`
- Skips tokens until semicolon or end of file
- Allows parsing to continue for detecting multiple errors

## Example: Parsing Flow

Consider parsing the program:
```
var x : integer is 5;
routine foo(a : integer) : integer is
    return a + x;
end
```

**Execution flow:**

1. **ProgramNode.Parse()** reads `var` → creates `VarNode`
    - `VarNode.Parse()` creates `PrimaryNode("x")`, `ExpressionNode` for `5`
    - `ExpressionNode.Parse()` creates `PrimaryNode(5)`

2. **ProgramNode.Parse()** reads `routine` → creates `RoutineNode`
    - `RoutineNode.Parse()` creates `PrimaryNode("foo")`
    - Reads parameters → creates `ParameterNode`
        - `ParameterNode.Parse()` creates `PrimaryNode("a")`, `PrimaryNode("integer")`
    - Reads body → creates `ProgramNode` (recursion!)
        - `ProgramNode.Parse()` reads `return` → creates `ReturnNode`
            - `ReturnNode.Parse()` creates `ExpressionNode` for `a + x`
                - `ExpressionNode.Parse()` applies Shunting-yard:
                    - Creates `PrimaryNode("a")`, `PrimaryNode("x")`
                    - Creates `OperationNode("+")` with two operands

**Result:** Fully constructed AST tree with all nested nodes.

## Key Features

### Recursive Structure
`ProgramNode` can be invoked recursively to parse nested scopes (function bodies, loops, conditional statements).

### Lookahead Strategy
To resolve ambiguity, 1-token lookahead is used via `tokenQueue.ElementAt(1)`:
- Distinguishing function calls from assignments
- Determining field access type (dot vs brackets)

### Token Queue Management
All nodes work with a shared token queue passed by reference (`ref Queue<Token>`). Each node:
- Extracts needed tokens via `Dequeue()`
- Peeks at next token via `Peek()` without extraction
- Passes queue to child nodes to continue parsing