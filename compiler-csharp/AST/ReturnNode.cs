using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class ReturnNode : Node {
	public ReturnNode(Position pos) : base(pos) { }


	public override void Parse(ref Queue<Token> tokenQueue) {
		ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ReturnNode") Console.WriteLine($"ReturnNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}

    public override void Verify() {
        if (!SymbolTable.IsInsideType(ScopeType.Routine)) {
            ErrorHandling.Add("ReturnNode", this.position, "'return' used outside routine");
            return;
        }

        base.Verify();
    }
}
