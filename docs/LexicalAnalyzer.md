# LexicalAnalyzer documentation

## Content

- [Transitions](#transitions)
- [`Lexer` Class](#lexer-class)
- [States](#states)
    - [`State` Class](#abstract-state-class)
    - [`StoringState` Class](#abstract-storingstate-class)
    - [`ChoosingState` Class](#abstract-choosingstate-class)
    - [Other states classes](#other-states-classes)

## General Idea of Lexical Analyzer

The main idea of our lexical analyzer is that we read the source file character by character. Depending on the received symbol, we change the state of our analysis.

## Transitions

**Transitions** determines how the states change depending on the received symbol. With this transitions, the lexer understands how to properly tokenize the processed code

States:
- Start
- Identifier
- Numeric
- Less
- Greater
- EqualOrOneliner
- DotOrRange
- DivOrNotEqual
- ColonOrAssignment


### Transitions from  `Start` state:
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

### Transitions from  `Identifier` state:
```
Identifier --(letter/digit/_)--> Identifier
Identifier --(<)--> Less
Identifier --(>)--> Greater
Identifier --(=)--> EqualOrOneliner
Identifier --(other)--> Error
```

### Transitions from  `Numeral` state:
```
Numeral --(digit)--> Numeral
Numeral --(letter)--> Identifier
Numeral --(<)--> Less
Numeral --(>)--> Greater
Numeral --(=)--> EqualOrOneliner
Numeral --(other)--> Error
```

### Transitions from  `Less` state:
```
Less --(=)--> Start
Less --(digit)--> Numeral
Less --(letter)--> Identifier
Less --(<)--> Less
Less --(>)--> Greater
Less --(other)--> Error
```

### Transitions from  `Greater` state:
```
Greater --(=)--> Start
Greater --(digit)--> Numeral
Greater --(letter)--> Identifier
Greater --(<)--> Less
Greater --(>)--> Greater
Greater --(other)--> Error
```

### Transitions from  `EqualOrOneliner` state:
```
EqualOrOneliner --(>)--> Start
EqualOrOneliner --(=)--> Start
EqualOrOneliner --(digit)--> Numeral
EqualOrOneliner --(letter)--> Identifier
EqualOrOneliner --(<)--> Less
EqualOrOneliner --(other)--> Error
```

### Transitions from  `DivOrNotEqual` state:
```
DivOrNotEqual --(=)--> Start
DivOrNotEqual --(digit)--> Numeral
DivOrNotEqual --(letter)--> Identifier
DivOrNotEqual --(<)--> Less
DivOrNotEqual --(>)--> Greater
DivOrNotEqual --(other)--> Error
```

### Transitions from  `ColonOrAssignment` state:
```
ColonOrAssignment --(=)--> Start
ColonOrAssignment --(digit)--> Numeral
ColonOrAssignment --(letter)--> Identifier
ColonOrAssignment --(<)--> Less
ColonOrAssignment --(>)--> Greater
ColonOrAssignment --(other)--> Error
```

### Transitions from  `DotOrRange` state:
```
DotOrRange --(.)--> Start
DotOrRange --(digit)--> Numeral
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
- NumeralState
- LessState
- GreaterState
- EqualState
- DotOrRangeState
- DivOrNotEqualState
- ColonOrAssignmentState

Those are implementations of each lexer state, which are inherited from `State`, `StoringState`, or `ChoosingState` classes. To check out implementations, follow [LexicalAnalyzer.cs](../compiler-csharp/LexicalAnalyzer.cs) file