# Lexical Analysis

## Overview

Lexical analysis transforms source code character stream into a sequence of tokens. The lexer is implemented as a Finite State Machine (FSM) that processes characters one at a time, transitioning between states and emitting tokens when complete lexemes are recognized.

## Entry Point

Lexical analysis is invoked in `Program.Main()`:

```csharp
ErrorHandling.ChangeStage("Lexical analysis");
Queue<Token> stream = new Queue<Token>();
Lexer lexer = new Lexer();
lexer.ParseFile(ref stream);
```

## Architecture

### Token Structure

Each token contains three components:

```csharp
public class Token {
    private Position position;     // Source location (row, col)
    private TokenCode tokenCode;   // Token category
    private object value;          // Lexeme value (optional)
}
```

### Token Categories

The `TokenCode` enum defines all recognized token types:

- **Built-in types**: `integer`, `real`, `boolean`
- **Literals**: `constant_value` (numbers, true/false)
- **Identifiers**: user-defined names
- **Operators**: `logic_op` (and/or/xor/not), `relation_op` (<, <=, >, >=, ==, /=), `factor_op` (*, /, %), `term_op` (+, -)
- **Delimiters**: parentheses, brackets, separators (comma, semicolon, dot)
- **Keywords**: var, type, if, while, for, routine, return, etc.
- **Assignments**: `:` (type_assignment), `:=` (bare_assignment), `is` (is_assignment)
- **Special**: `=>` (one_line_body), `..` (range_sign), `end` (end_of_body)

### Keyword Recognition

Reserved words are stored in `DedicatedWords` dictionary:
- Keywords like `var`, `if`, `while`, `routine` map to specific token codes
- Built-in types: `integer`, `real`, `boolean`
- Boolean literals: `true`, `false`
- Logical operators: `and`, `or`, `xor`, `not`

During identifier recognition, lexemes are checked against this dictionary to distinguish keywords from user identifiers.

### Immediate Separators

Single-character operators/delimiters recognized instantly:
- `,` → `comma`
- `(` → `left_parenthesis`
- `)` → `right_parenthesis`
- `[` → `left_bracket`
- `]` → `right_bracket`
- `*` → `factor_op`
- `%` → `factor_op`
- `+` → `term_op`
- `-` → `term_op`
- `;` → `semicolon`

These emit tokens immediately without state machine processing.

## Finite State Machine

### State Diagram

The FSM consists of 10 states:

**States:**
- `Start` - initial/reset state
- `Identifier` - accumulating letters/digits/_
- `Numeric` - accumulating integer or real number
- `Less` - processing `<` or `<=`
- `Greater` - processing `>` or `>=`
- `EqualOrOneliner` - processing `=`, `==`, or `=>`
- `ColonOrAssignment` - processing `:` or `:=`
- `DotOrRange` - processing `.` or `..`
- `DivOrNotEqual` - processing `/` or `/=`
- `Error` - invalid character sequence

### State Transitions

#### Start State

```
Start --(letter)--> Identifier
Start --(digit)--> Numeric
Start --(< )--> Less
Start --(> )--> Greater
Start --(= )--> EqualOrOneliner
Start --(other)--> Error
```

Special handling in `ParseFile`:
```
Start --(separator)--> Start [emit separator token]
Start --(whitespace)--> Start
Start --(newline)--> Start [increment line counter]
Start --(:)--> ColonOrAssignment
Start --(/ )--> DivOrNotEqual
Start --(. )--> DotOrRange
```

#### Identifier State

```
Identifier --(letter)--> Identifier [accumulate]
Identifier --(digit)--> Identifier [accumulate]
Identifier --(_ )--> Identifier [accumulate]
Identifier --(< )--> Less [emit identifier token]
Identifier --(> )--> Greater [emit identifier token]
Identifier --(= )--> EqualOrOneliner [emit identifier token]
Identifier --(other)--> Error
```

When emitting token:
- Check if lexeme is keyword → emit with keyword token code
- Check if `true`/`false` → emit `constant_value` with boolean value
- Otherwise → emit as `identifier`

#### Numeric State

```
Numeric --(digit)--> Numeric [accumulate]
Numeric --(. , first)--> Numeric [accumulate with ',', mark as real]
Numeric --(. , second)--> DotOrRange [emit number token]
Numeric --(letter)--> Identifier [emit number token]
Numeric --(< )--> Less [emit number token]
Numeric --(> )--> Greater [emit number token]
Numeric --(= )--> EqualOrOneliner [emit number token]
Numeric --(other)--> Error
```

When emitting token:
- If marked as real → parse as `float`, emit `constant_value`
- Otherwise → parse as `int`, emit `constant_value`

#### Less State

```
Less --(= )--> Start [emit "<=" relation_op]
Less --(digit)--> Numeric [emit "<" relation_op]
Less --(letter)--> Identifier [emit "<" relation_op]
Less --(< )--> Error
Less --(> )--> Greater [emit "<" relation_op]
Less --(other)--> Error
```

#### Greater State

```
Greater --(= )--> Start [emit ">=" relation_op]
Greater --(digit)--> Numeric [emit ">" relation_op]
Greater --(letter)--> Identifier [emit ">" relation_op]
Greater --(< )--> Less [emit ">" relation_op]
Greater --(> )--> Greater [emit ">" relation_op]
Greater --(other)--> Error
```

#### EqualOrOneliner State

```
EqualOrOneliner --(> )--> Start [emit "=>" one_line_body]
EqualOrOneliner --(= )--> Start [emit "==" relation_op]
EqualOrOneliner --(digit)--> Numeric [error: standalone '=' invalid]
EqualOrOneliner --(letter)--> Identifier [error: standalone '=' invalid]
EqualOrOneliner --(< )--> Less [error: standalone '=' invalid]
EqualOrOneliner --(other)--> Error
```

Note: Single `=` is invalid and triggers error during token emission.

#### DivOrNotEqual State

```
DivOrNotEqual --(= )--> Start [emit "/=" relation_op]
DivOrNotEqual --(digit)--> Numeric [emit "/" factor_op]
DivOrNotEqual --(letter)--> Identifier [emit "/" factor_op]
DivOrNotEqual --(< )--> Less [emit "/" factor_op]
DivOrNotEqual --(> )--> Greater [emit "/" factor_op]
DivOrNotEqual --(other)--> Error
```

#### ColonOrAssignment State

```
ColonOrAssignment --(= )--> Start [emit ":=" bare_assignment]
ColonOrAssignment --(digit)--> Numeric [emit ":" type_assignment]
ColonOrAssignment --(letter)--> Identifier [emit ":" type_assignment]
ColonOrAssignment --(< )--> Less [emit ":" type_assignment]
ColonOrAssignment --(> )--> Greater [emit ":" type_assignment]
ColonOrAssignment --(other)--> Error
```

#### DotOrRange State

```
DotOrRange --(. )--> Start [emit ".." range_sign]
DotOrRange --(digit)--> Numeric [emit "." dot]
DotOrRange --(letter)--> Identifier [emit "." dot]
DotOrRange --(= )--> EqualOrOneliner [emit "." dot]
DotOrRange --(< )--> Less [emit "." dot]
DotOrRange --(> )--> Greater [emit "." dot]
DotOrRange --(other)--> Error
```

Special handling in `ParseFile`:
- If current state is `Numeric` and `.` encountered → stay in `Numeric` (decimal point)
- If `.` followed by `.` → enter `DotOrRange` for range operator

#### Error State

```
Error --(any)--> Start [report error, reset state]
```

### Total Transition Table

| Current State     | letter     | digit      | \<    | \>      | =               | :                 | /             | .                     | _          | whitespace | separator | other |
| ----------------- | ---------- | ---------- |-------|---------| --------------- | ----------------- | ------------- | --------------------- | ---------- | ---------- | --------- | ----- |
| Start             | Identifier | Numeric    | Less  | Greater | EqualOrOneliner | ColonOrAssignment | DivOrNotEqual | DotOrRange            | Error      | Start      | Start     | Error |
| Identifier        | Identifier | Identifier | Less  | Greater | EqualOrOneliner | Error             | Error         | Error                 | Identifier | Start      | Start     | Error |
| Numeric           | Identifier | Numeric    | Less  | Greater | EqualOrOneliner | Error             | Error         | Numeric* / DotOrRange | Error      | Start      | Start     | Error |
| Less              | Identifier | Numeric    | Error | Greater | Error           | Error             | Error         | Error                 | Error      | Start      | Start     | Error |
| Greater           | Identifier | Numeric    | Less  | Error   | Error           | Error             | Error         | Error                 | Error      | Start      | Start     | Error |
| EqualOrOneliner   | Identifier | Numeric    | Less  | Start   | Start           | Error             | Error         | Error                 | Error      | Error      | Error     | Error |
| DivOrNotEqual     | Identifier | Numeric    | Less  | Greater | Start           | Error             | Error         | Error                 | Error      | Start      | Start     | Error |
| ColonOrAssignment | Identifier | Numeric    | Less  | Greater | Start           | Error             | Error         | Error                 | Error      | Start      | Start     | Error |
| DotOrRange        | Identifier | Numeric    | Less  | Greater | EqualOrOneliner | Error             | Error         | Start                 | Error      | Start      | Start     | Error |
| Error             | Start      | Start      | Start | Start   | Start           | Start             | Start         | Start                 | Start      | Start      | Start     | Start |

## Tokenization Process

### Main Loop

The `ParseFile` method processes characters sequentially:

1. **Read character** from `FileReader`
2. **Check for immediate separators** - emit token and continue
3. **Check for whitespace/newline** - skip and continue
4. **Check for special multi-character starts** (`:`, `/`, `.`) - handle appropriately
5. **Process through current state** - call `ProccessSymbol()`
6. **Handle state transitions**:
    - If state changed and not separator → emit accumulated token
    - Create new state object with appropriate data
    - Update `currentState` and `currentStateCode`
7. **Advance position** tracker
8. **Repeat** until EOF
9. **Emit final token** if state is not `Start`
10. **Append EOF token**

### Position Tracking

`Position` structure tracks source location for error reporting:
- `row` - current line number (1-indexed)
- `col` - current column number (0-indexed)
- `NextLine()` - increments row, resets column
- `NextChar()` - increments column

### State Object Pattern

Each state is represented by a class inheriting from abstract `State`:

**Abstract Base:**
```csharp
public abstract class State {
    protected Position position;
    public abstract StateCode ProccessSymbol(char symbol);
    public abstract void AddToken(ref Queue<Token> tokenQueue);
}
```

**Concrete States:**
- `StartState` - no data, doesn't emit tokens
- `StoringState` (abstract) - accumulates characters in `data` string
    - `IdentifierState` - stores letters/digits/_
    - `NumeralState` - stores digits and decimal point
- `ChoosingState` (abstract) - tracks single vs multi-character with `single` flag
    - `LessState` - emits `<` or `<=`
    - `GreaterState` - emits `>` or `>=`
    - `EqualState` - emits `=>` or `==`
    - `DivOrNotEqualState` - emits `/` or `/=`
    - `ColonOrAssignmentState` - emits `:` or `:=`
    - `DotOrRangeState` - emits `.` or `..`

### Real Number Recognition

Numeric state handles decimal point specially:
- First `.` → converts to `,` (decimal separator), sets `real = true`, stays in `Numeric`
- Second `.` → treats as range operator, transitions to `DotOrRange`
- When emitting: if `real == true`, parses as `float`; otherwise as `int`

### Error Handling

Invalid character sequences trigger error reporting:
- Transition to `Error` state
- Call `ErrorHandling.UnknownSymbol(position)`
- Reset to `Start` state and continue parsing
- Allows detecting multiple errors in single pass

## Example Tokenization

**Input:** `var x : integer := 5;`

**Processing:**
```
v → Start --(letter)--> Identifier ["v"]
a → Identifier --(letter)--> Identifier ["va"]
r → Identifier --(letter)--> Identifier ["var"]
  → Identifier --(whitespace)--> Start [emit "var" as variable_declaration]
x → Start --(letter)--> Identifier ["x"]
  → Identifier --(whitespace)--> Start [emit "x" as identifier]
: → Start --(:)--> ColonOrAssignment
  → ColonOrAssignment --(whitespace)--> Start [emit ":" as type_assignment]
i → Start --(letter)--> Identifier ["i"]
n → Identifier --(letter)--> Identifier ["in"]
t → Identifier --(letter)--> Identifier ["int"]
... → Identifier ["integer"]
  → Identifier --(whitespace)--> Start [emit "integer" as builtin_type]
: → Start --(:)--> ColonOrAssignment
= → ColonOrAssignment --(=)--> Start [emit ":=" as bare_assignment]
  → Start --(whitespace)--> Start
5 → Start --(digit)--> Numeric ["5"]
; → Numeric --(separator)--> Start [emit 5 as constant_value, emit ";" as semicolon]
```

**Output Tokens:**
1. `(1,0) variable_declaration "var"`
2. `(1,4) identifier "x"`
3. `(1,6) type_assignment ":"`
4. `(1,8) builtin_type "integer"`
5. `(1,16) bare_assignment ":="`
6. `(1,19) constant_value 5`
7. `(1,20) semicolon ";"`
8. `(1,21) end_of_file`

## Key Features

### Maximal Munch Principle

Lexer consumes longest possible lexeme before emitting token:
- `<=` recognized as single relation operator, not `<` followed by `=`
- Identifiers accumulate all consecutive letters/digits/underscores
- Numbers accumulate all digits and optional decimal point

### Lookahead Support

`FileReader.Peek()` enables single-character lookahead:
- Used to detect `..` range operator (two consecutive dots)
- Prevents incorrect decimal point interpretation

### Keyword vs Identifier Disambiguation

After accumulating identifier, checks `DedicatedWords` dictionary:
- If lexeme matches keyword → emit with keyword token code
- Otherwise → emit as generic `identifier`

### Type-Aware Value Storage

Token values stored with appropriate types:
- Boolean literals: `true`/`false` → `bool` value
- Integer literals: `123` → `int` value
- Real literals: `3.14` → `float` value
- Keywords/identifiers: stored as `string`
- Operators: stored as `string` representation