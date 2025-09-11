# Lexer.FSM namespace documentation

## Content

- [Transitions Table](#transitions-table)
- [`Lexer` Class](#fsm-class)
- [`State` Class](#abstract-state-class)
- [`StoringState` Class](#abstract-storingstate-class)
- [`ChoosingState` Class](#abstract-choosingstate-class)

## Transitions Table

The **transition table** determines how the states change depending on the received symbol. With this transition table, the FSM lexer understands how to properly tokenize the processed code

---

## `Lexer` Class

**`Lexer` class** implements a state change depending on the received symbol

### Accepted arguments

- `FileReader reader` - check [FileReader](IO.md#filereader-class) class. Used to read all file
- `Queue<Token> stream` - streaming variable to keep tokens

### Class fields

- `private State currentState` - defines current FSM state, used for state switching
- `private StateCode currentLexerState` - enum state, which defines current FSM state, used for state comparisons
- `public Queue<Token> TokenStream` - queue which will keep tokens 
- `public FileReader reader` - to read file char-by-char


### Class methods

- `public Lexer(FileReader reader, Queue<Token>)` - class constructor, defining initial state and fields
- `public void ParseFile()` - method, which reads and tokenize file

---

## Abstract `State` Class

**`State` class** is an abstract class from which state classes are inherited

### Accepted arguments

Doesn't accept any argument at the moment

### Class methods

- `public abstract StateCode ProcessSymbol(char symbol)` - process an incoming character. Depending on symbol, changing data or next state
- `public abstract Token CreateToken()` - returns data in convenient format to create a token

---

## Abstract `StoringState` class

**`StoringState` class** is an abstract class for inheritance for state classes which accumulate its data

### Accepted arguments

Doesn't accept any argument at the moment

### Class fields

- `protected string data` - variable defined to store some data

### Class methods

- `protected StoringState()` - class constructor which defines initial data

### Inheritance

Inherits from [`State`](#abstract-state-class) class

---

## Abstract `ChoosingState` class

**`ChoosingState` class** is an abstract class for inheritance for state classes which have unidentified final state

### Class fields

- `protected bool single` - defines whether state operator is a single char or not

### Class methods

- `protected ChoosingState()` - defines initial state of `single` field

### Inheritance

Inherits from [`State`](#abstract-state-class) class

