using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class RecordNode : Node {
	public RecordNode(Position pos) : base(pos) { }

	public override void Parse(ref Queue<Token> tokenQueue) {
		Token token = tokenQueue.Peek();
		while(token.Code() != TokenCode.end_of_body) {
			if(token.Code() != TokenCode.variable_declaration) {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
			tokenQueue.Dequeue();
			VarNode field = new VarNode(token.Position());
			field.Parse(ref tokenQueue);
			this.childs.Add(field);
			token = tokenQueue.Peek();
		}
		tokenQueue.Dequeue();
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "RecordNode") Console.WriteLine($"RecordNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}

    public override void Verify() {
        foreach (var child in childs) {
            if (child is not VarNode) {
                ErrorHandling.Add("RecordNode", this.position, $"RecordNode should contain only VarNodes");
                return;
            }
        }
        base.Verify();
    }

    public override void Unuse() {
        string identifier = (string)((PrimaryNode)this.childs[0]).value;
        SymbolTable.UnuseEntry(identifier);
        base.Unuse();
    }
}
