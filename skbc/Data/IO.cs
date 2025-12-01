using System.Text;
namespace Compiler.Data;
public static class FileReader {
	private static string _filename;
	private static StreamReader _fileStream;
	public static string filename => _filename;

	public static void SetFile(string filepath) {
		// Check if file exists
		if (!File.Exists(filepath))
			throw new FileNotFoundException("File not found: " + filepath);

		// Remember filename
		_filename = Path.GetFileName(filepath);

		// Read file content and process it
		_fileStream = new StreamReader(filepath, Encoding.ASCII);
	}

	public static bool Empty() =>
		_fileStream.Peek() == -1;

	public static char Get() {
		int code = _fileStream.Read();
		if(code == 10) return '\n';
		char symbol = (char)code;
		return symbol;
	}

	public static char Peek() {
		int code = _fileStream.Peek();
		if(code == 10) return '\n';
		char symbol = (char)code;
		return symbol;
	}
}
