using System;
using System.IO;

namespace Lexer.IO
{
    public class FileReader
    {
        private string _filename;
        private StreamReader _fileStream;

        public FileReader(string filepath)
        {
            // Check if file exists
            if (!File.Exists(filepath))
            {
                Console.WriteLine("File not found: " + filepath);
                return;
            }

            // Remember filename
            this._filename = Path.GetFileName(filepath);

            // Read file content and process it
            this._fileStream = new StreamReader(filepath);
        }

        public string GetFilename()
        {
            return this._filename;
        }

        public char? GetNextChar()
        {
            int nextChar = this._fileStream.Read();
            if (nextChar == -1) return null; // End of file
            return (char)nextChar;
        }

    }
}