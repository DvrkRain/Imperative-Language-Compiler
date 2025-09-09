using System;
using System.IO;

namespace Lexer.IO
{
    public class FileReader
    {
        private string _filename;
        private List<string> _content = new List<string>();

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
            string[] contentLines = File.ReadAllText(filepath).Split("\n"); // Split into lines
            foreach (string line in contentLines)
            {
                int indexOfComment = line.IndexOf("//"); 
                string processedLine = indexOfComment >= 0 ? line.Substring(0, indexOfComment) : line; // Remove comments if any

                string[] words = processedLine.Split(new char[] { ' ', '\t', '\r' }, StringSplitOptions.RemoveEmptyEntries); // Split into words
                foreach (string word in words)
                {
                    this._content.Add(word);
                }
            }
        }
        
        public string GetFilename()
        {
            return this._filename;
        }

        public List<string> GetContent()
        {
            return this._content;
        }

    }
}