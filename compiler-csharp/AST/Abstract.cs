using Data.Objects;
using Data.ErrorHandling;

namespace AST;
public abstract class Node {
    protected Position position{get; set;}
	protected string _type;
    protected List<Node> childs;

    public List<Node> GetChilds() => this.childs;
	public string Type() => this._type;
	public void Type(string str) => this._type = str;

	public Position Position() =>
		this.position;

    private Node() {
		this.childs = new List<Node>();
		this._type = "void";
	}
    protected Node(Position pos) : this() => this.position = pos;

    protected void HandleUnexpectedToken(ref Queue<Token> tokenQueue, Position pos, TokenCode got, string expected) {
        ErrorHandling.UnexpectedToken(this.GetType().Name, pos, got, expected);
        Token token = tokenQueue.Peek();
        while (token.Code() != TokenCode.semicolon && tokenQueue.Count > 0) {
            if (token.Code() == TokenCode.end_of_file) {
                ErrorHandling.UnexpectedEOF(this.GetType().Name, token.Position());
                break;
            }
            tokenQueue.Dequeue();
            token = tokenQueue.Peek();
        }
    }

    public virtual void PrintInfo(string indent) {
        for (int i = 0; i < this.childs.Count; i++) {
            if (i == this.childs.Count - 1) {
                Console.Write(indent + "L--");
                this.childs[i].PrintInfo(indent + "   ");
            } else {
                Console.Write(indent + "+--");
                this.childs[i].PrintInfo(indent + "|  ");
            }
        }
    }


    public abstract void Parse(ref Queue<Token> tokenQueue);


	public virtual void Verify() {
		foreach(var child in childs)
			child.Verify();
	}


    public virtual void Generate(CodeGen.CodeGenContext context) {
        foreach(var child in childs)
            child.Generate(context);
    }
}
