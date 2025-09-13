# Lexer.IO namespace documentation

## Content

- [FileReader](#filereader-class)

## FileReader class

**FileReader** is a class for char-by-char reading of source files. It provides a simple interface for sequential access to the contents of a file, which is ideal for a lexical analyzer that processes the source code character by character.

### Properties
- **`filename`** (string, read-only) - returns the name of the current file being read (without full path)

### Constructor
- **`public FileReader(string filepath)`** - initializes a new instance of FileReader
  - Checks if the file exists at the specified path
  - Stores the filename (without path) for reference
  - Initializes a StreamReader with ASCII encoding to read the file

### Methods
- **`public bool Empty()`** - checks if the end of the file has been reached
  - Returns `true` if there are no more characters to read, `false` otherwise
  - Uses `Peek()` method to check without advancing the position

- **`public char GetNextChar()`** - reads the next character from the file
  - Returns the next character from the input stream
  - Converts line feed (ASCII 10) to newline character (`'\n'`)
  - Advances the read position in the file
  - Note: Does not return null on EOF - use `Empty()` to check for end of file

### Exceptions
- **`FileNotFoundException`** - thrown during construction if the specified file doesn't exist

### Usage Example
```csharp
var reader = new FileReader("example.txt");
while (!reader.Empty())
{
    char c = reader.GetNextChar();
    // Process character
}
```
