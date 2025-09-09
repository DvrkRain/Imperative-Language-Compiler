# Lexer.IO namespace documentation

## Content

- [FileReader](#filereader-class)

## FileReader class

**FileReader** is a class for char-by-char reading of source files. It provides a simple interface for sequential access to the contents of a file, which is ideal for a lexical analyzer that processes the source code character by character

### Accepted arguments
- **filepath** (string) - the path to the file to be read. The constructor checks the existence of the file and initializes the stream for reading.

## Class methods
- **`public FileReader(string filepath)`** - constructor, which checks file existence and initializes the `StreamReader' to read the file
- **`public string getFileName()`** - returns the name of the current file
- **`public char? GetNextChar()`** - reads following character from the file, returns `null` when EOF, `char` otherwise
