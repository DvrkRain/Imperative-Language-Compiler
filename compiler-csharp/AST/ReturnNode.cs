using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class ReturnNode : Node {
    public ReturnNode(Position pos, string returnType = "void") : base(pos) =>
		this._type = returnType;

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ReturnNode") Console.WriteLine($"ReturnNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		if(this._type != "void") {
			ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
		}

		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();
	}


    public override void Verify() {
        if (!SymbolTable.IsInsideType(ScopeType.Routine)) {
            ErrorHandling.Add("ReturnNode", this.position, "'return' used outside routine");
            return;
        }
        
        base.Verify();

        if (this.childs[0] is not ExpressionNode) {
            ErrorHandling.Add("ReturnNode", this.position, "return doesn't have expression");
            return;
        }
        
        ExpressionNode returnExpression = (ExpressionNode)this.childs[0];

        if (this._type != "void" && this._type != returnExpression.Type()) {
            ErrorHandling.Add("ReturnNode", this.position, $"return type mismatch {this._type} != {returnExpression.Type()}");
            return;
        }
    }
}
