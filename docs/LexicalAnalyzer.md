# LexicalAnalyzer namespace documentation

## Content

- [Transitions](#transitions)
- [`Lexer` Class](#lexer-class)
- [States](#states)
    - [`State` Class](#abstract-state-class)
    - [`StoringState` Class](#abstract-storingstate-class)
    - [`ChoosingState` Class](#abstract-choosingstate-class)
    - [Other states classes](#other-states-classes)

## Transitions

**Transitions** determines how the states change depending on the received symbol. With this transitions, the lexer understands how to properly tokenize the processed code

States:
- StartState
- IdentifierState
- IntegerState
- LessState
- GreaterState
- EqualState
- DotOrRangeState
- DivOrNotEqualState
- ColonOrAssignmentState


### Переходы из StartState:
```
Start --(letter)--> Identifier
Start --(digit)--> Integer
Start --(<)--> Less
Start --(>)--> Greater
Start --(=)--> EqualOrOneliner
Start --(:)--> ColonOrAssignment
Start --(/)--> DivOrNotEqual
Start --(.)--> DotOrRange
Start --(other)--> Error
```

### Переходы из IdentifierState:
```
Identifier --(letter/digit/_)--> Identifier
Identifier --(<)--> Less
Identifier --(>)--> Greater
Identifier --(=)--> EqualOrOneliner
Identifier --(other)--> Error
```

### Переходы из IntegerState:
```
Integer --(digit)--> Integer
Integer --(letter)--> Identifier
Integer --(<)--> Less
Integer --(>)--> Greater
Integer --(=)--> EqualOrOneliner
Integer --(other)--> Error
```

### Переходы из LessState:
```
Less --(=)--> Start
Less --(digit)--> Integer
Less --(letter)--> Identifier
Less --(<)--> Less
Less --(>)--> Greater
Less --(other)--> Error
```

### Переходы из GreaterState:
```
Greater --(=)--> Start
Greater --(digit)--> Integer
Greater --(letter)--> Identifier
Greater --(<)--> Less
Greater --(>)--> Greater
Greater --(other)--> Error
```

### Переходы из EqualOrOnelinerState:
```
EqualOrOneliner --(>)--> Start
EqualOrOneliner --(=)--> Start
EqualOrOneliner --(digit)--> Integer
EqualOrOneliner --(letter)--> Identifier
EqualOrOneliner --(<)--> Less
EqualOrOneliner --(other)--> Error
```

### Переходы из DivOrNotEqualState:
```
DivOrNotEqual --(=)--> Start
DivOrNotEqual --(digit)--> Integer
DivOrNotEqual --(letter)--> Identifier
DivOrNotEqual --(<)--> Less
DivOrNotEqual --(>)--> Greater
DivOrNotEqual --(other)--> Error
```

### Переходы из ColonOrAssignmentState:
```
ColonOrAssignment --(=)--> Start
ColonOrAssignment --(digit)--> Integer
ColonOrAssignment --(letter)--> Identifier
ColonOrAssignment --(<)--> Less
ColonOrAssignment --(>)--> Greater
ColonOrAssignment --(other)--> Error
```

### Переходы из DotOrRangeState:
```
DotOrRange --(.)--> Start
DotOrRange --(digit)--> Integer
DotOrRange --(letter)--> Identifier
DotOrRange --(=)--> EqualOrOneliner
DotOrRange --(<)--> Less
DotOrRange --(>)--> Greater
DotOrRange --(other)--> Error
```

### Special Transitions (from ParseFile):
```
AnyState --(separator)--> Start (with separator token creation)
AnyState --(\n)--> Start (with line increment)
AnyState --(whitespace)--> Start
AnyState --(:)--> ColonOrAssignment (with token completion if already in ColonOrAssignment)
AnyState --(/)--> DivOrNotEqual (with token completion if already in DivOrNotEqual)
AnyState --(.)--> DotOrRange (only if not already in DotOrRange)
Error --(any)--> Start
```

---

## `Lexer` Class

**`Lexer` class** implements a state change depending on the received symbol

### Accepted arguments

- `FileReader reader` - check [FileReader](IO.md#filereader-class) class. Used to read all file
- `Queue<Token> stream` - streaming variable to keep tokens

### Class fields

- `private State currentState` - defines current Lexer state, used for state switching
- `private StateCode currentStateCode` - enum state, which defines current state, used for state comparisons
- `public Queue<Token> TokenStream` - queue which will keep tokens 
- `public FileReader reader` - to read file char-by-char
- `private static List<char> separators` - list, including separator characters


### Class methods

- `public Lexer(FileReader reader, Queue<Token>)` - class constructor, defining initial state and fields
- `public void ParseFile()` - method, which reads file char-by-char, changes states, and tokenize source code

### Exceptions

Throws `UnexpectedTokenException` when meets unidentified token 

---

## States

### Abstract `State` Class

**`State` class** is an abstract class from which state classes are inherited

#### Accepted arguments

Doesn't accept any argument at the moment

#### Class methods

- `public abstract StateCode ProcessSymbol(char symbol)` - process an incoming character. Depending on symbol, changing data or next state
- `public abstract Token CreateToken()` - returns data in convenient format to create a token

---

### Abstract `StoringState` class

**`StoringState` class** is an abstract class for inheritance for state classes which accumulate its data

#### Accepted arguments

Doesn't accept any argument at the moment

#### Class fields

- `protected string data` - variable defined to store some data

#### Class methods

- `protected StoringState()` - class constructor which defines initial data

#### Inheritance

Inherits from [`State`](#abstract-state-class) class

---

### Abstract `ChoosingState` class

**`ChoosingState` class** is an abstract class for inheritance for state classes which have unidentified final state

#### Class fields

- `protected bool single` - defines whether state operator is a single char or not

#### Class methods

- `protected ChoosingState()` - defines initial state of `single` field

#### Inheritance

Inherits from [`State`](#abstract-state-class) class


### Other states classes

Other states include:
- StartState
- IdentifierState
- IntegerState
- LessState
- GreaterState
- EqualState
- DotOrRangeState
- DivOrNotEqualState
- ColonOrAssignmentState

Those are implementations of each lexer state, which are inherited from `State`, `StoringState`, or `ChoosingState` classes. To check out implementations, follow [LexicalAnalyzer.cs](../compiler-csharp/LexicalAnalyzer.cs) file