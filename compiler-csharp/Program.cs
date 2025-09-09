using System;
using Lexer.IO;

namespace Compiler
{
  class Program
  {
    static void Main(string[] args)
    {
      if (args.Length < 1)
      {
        Console.WriteLine("Usage: compiler-csharp <source-file-path>");
        return;
      }

      string filepath = args[0];
      FileReader reader = new FileReader(filepath);
      Console.WriteLine("Filename: " + reader.GetFilename());
      while (true)
      {
        char? nextChar = reader.GetNextChar();
        if (nextChar == null) break;
        Console.Write(nextChar);
      }
    }
    
  }
}
