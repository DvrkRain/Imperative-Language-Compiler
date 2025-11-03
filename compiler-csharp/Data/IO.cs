using System.Text;
namespace Data.IO
{
    public class FileReader
    {
        private string _filename;
        private StreamReader _fileStream;
		public string filename => _filename;

        public FileReader(string filepath)
        {
            // Check if file exists
            if (!File.Exists(filepath))
            {
                throw new FileNotFoundException("File not found: " + filepath);
            }

            // Remember filename
            this._filename = Path.GetFileName(filepath);

            // Read file content and process it
            this._fileStream = new StreamReader(filepath, Encoding.ASCII);
        }

		public bool Empty() {
			return _fileStream.Peek() == -1;
		}

        public char GetNextChar()
        {
			int code = _fileStream.Read();
			if(code == 10) return '\n';
			char symbol = (char)code;
			return symbol;
        }

    }
}
