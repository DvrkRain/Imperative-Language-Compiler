using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public abstract class Node {
    protected Position position;

    protected List<Node> childs;

    private Node() => this.childs = new List<Node>();
    protected Node(Position pos) : this() => this.position = pos;

    public List<Node> GetChilds() => this.childs;

    protected void HandleUnexpectedToken(ref Queue<Token> tokenQueue, Position pos) {
        ErrorHandling.UnexpectedTokenException(pos, this.GetType().Name);
        Token token = tokenQueue.Peek();
        while (token.Code() != TokenCode.semicolon && tokenQueue.Count > 0) {
            if (token.Code() == TokenCode.end_of_file) {
                ErrorHandling.UnexpectedEOF(token.Position(), this.GetType().Name);
                break;
            }

            tokenQueue.Dequeue();
            token = tokenQueue.Peek();
        }
    }

    public virtual void PrintInfo(string indent) {
        if (this.GetType().Name == "Node") Console.WriteLine($"Node(childs={this.childs.Count})");

        for (int i = 0; i < this.childs.Count; i++) {
            if (i == this.childs.Count - 1) {
                Console.Write(indent + "└── ");
                this.childs[i].PrintInfo(indent + "    ");
            } else {
                Console.Write(indent + "├── ");
                this.childs[i].PrintInfo(indent + "│   ");
            }
        }
    }

    public abstract void Parse(ref Queue<Token> tokenQueue);
	public virtual void Verify(ref SymbolTable symTab) {
		foreach(var child in childs)
			child.Verify(ref symTab);
	}
}
