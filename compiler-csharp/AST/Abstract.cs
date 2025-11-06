using Data.Objects;
using Exceptions;
namespace AST {
public abstract class Node {
	protected Position position;

	protected List<Node> childs;

	private Node() => this.childs = new List<Node>();
	protected Node(Position pos) : this() => this.position = pos;

	public List<Node> GetChilds() => this.childs;

	public abstract void Parse(ref Queue<Token> tokenQueue);

	protected void HandleUnexpectedToken(ref Queue<Token> tokenQueue, Position pos) {
		Console.WriteLine($"Unexpected Token at {pos.Row()}, {pos.Col()},\t while parsing {this.GetType().Name}.");
		Token token = tokenQueue.Peek();
		while(token.Code() != TokenCode.semicolon && tokenQueue.Count > 0) {
			if(token.Code() == TokenCode.end_of_file) 
				throw new UnexpectedEndOfFile(token.Position(), this.GetType().Name);
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
		}
	}

	public virtual void PrintInfo(string indent) {
		if (this.GetType().Name == "Node") Console.WriteLine($"Node(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");

		for(int i=0; i<this.childs.Count; i++) {
			if(i == this.childs.Count - 1) {
				Console.Write(indent + "└── ");
				this.childs[i].PrintInfo(indent + "    ");
			} else {
				Console.Write(indent + "├── ");
				this.childs[i].PrintInfo(indent + "│   ");
			}
		}
	}

}
}
