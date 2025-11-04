using Data.Objects;
namespace AST {
public abstract class Node {
	protected Position position;

	protected List<Node> childs;

	private Node() => this.childs = new List<Node>();
	protected Node(Position pos) : this() => this.position = pos;

	public List<Node> GetChilds() => this.childs;

	public abstract void Parse(ref Queue<Token> tokenQueue);

	protected void HandleUnexpectedToken(ref Queue<Token> tokenQueue) {
		Console.WriteLine($"Unexpected Token at {this.position.Row()}, {this.position.Col()}.");
		Token token = tokenQueue.Peek();
		while(token.Code() != TokenCode.semicolon && tokenQueue.Count > 0) {
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
		}
	}

	public virtual void PrintInfo(string indent) {
		if (this.GetType().Name == "Node") Console.WriteLine($"Node(childs={this.childs.Count})");

		for(int i=0; i<this.childs.Count; i++) {
			if(i == this.childs.Count - 1)
				Console.Write(indent + "└── ");
			else
				Console.Write(indent + "├── ");
			this.childs[i].PrintInfo(indent + "|   ");
		}
	}

}
}
