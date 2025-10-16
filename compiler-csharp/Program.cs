using LexicalAnalyzer.IO;
using LexicalAnalyzer.TokenTree;
using LexicalAnalyzer;
using SyntaxAnalyzer;

namespace Compiler
{
	class Program
	{
		public static void PrintProgramTree(Node node, string indent = "", bool isLast = true) {
			// Выводим текущий узел
			Console.Write(indent);
			if (isLast)
			{
				Console.Write("└── ");
				indent += "    ";
			}
			else
			{
				Console.Write("├── ");
				indent += "│   ";
			}
			Console.WriteLine(node.GetType().Name);

			var children = node.GetChilds();
			for (int i = 0; i < children.Count; i++)
			{
				PrintProgramTree(children[i], indent, i == children.Count - 1);
			}
		}

		static void Main(string[] args)
		{
			if (args.Length < 2)
			{
				Console.WriteLine("Usage: compiler-csharp <source-file-path> <tree-option>");
				return;
			}

			string filepath = args[0];
			FileReader reader = new FileReader(filepath);
			Console.WriteLine("Filename: " + reader.filename);

			Queue<Token> stream = new Queue<Token>();

			Lexer lexer = new Lexer(reader, stream);
			lexer.ParseFile();

			int treeOption = int.Parse(args[1]);
			if (treeOption == 0){
				while (stream.Count > 0) {
					Token token = stream.Dequeue();
					token.PrintInfo();
				}

				return;
			}

			ProgramNode ProgramTree = new ProgramNode();
			ProgramTree.Parse(ref stream);

			PrintProgramTree(ProgramTree);
			Console.WriteLine();

			// Interactive

			List<Node> NodeStack = [ProgramTree];

			while (NodeStack.Count > 0) {
                Console.Write("Current NodeStack: [" + string.Join(", ", NodeStack.Select(node => node.GetType().Name)) + "]\n");
				List<Node> childs = NodeStack[NodeStack.Count - 1].GetChilds();
				
				Console.WriteLine("Choose where to go next:");
				Console.WriteLine("0. Go Back");
				for (int i = 0; i < childs.Count; i++) {
                    Console.WriteLine($"{1 + i}. {childs[i].GetType().Name}");
                }

				int option = 0;

				while (!int.TryParse(Console.ReadLine(), out option) && 0 <= option && option <= childs.Count + 1) {
                    Console.WriteLine("Not a number or wrong option!");
                }

				if (option == 0) NodeStack.RemoveAt(NodeStack.Count - 1);
				else NodeStack.Add(childs[option - 1]);
            }
		}
	}
}
