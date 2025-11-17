using Data.ErrorHandling;
using Data.Objects;
namespace AST;
public class PrintNode : Node {
	public PrintNode(Position pos) : base(pos) { }
	
	public override void Parse(ref Queue<Token> tokenQueue) {
		ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		Token token = tokenQueue.Peek();
		while(token.Code() == TokenCode.comma) {
			tokenQueue.Dequeue();
			expr = new ExpressionNode(tokenQueue.Peek().Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Peek();
		}

		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "PrintNode") Console.WriteLine($"PrintNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}

    public override void Verify() {
        base.Verify();

        foreach (var child in childs) {
            if (child is not ExpressionNode) {
                ErrorHandling.Add("PrintNode", this.position, $"PrintNode should contain only ExpressionNodes");
                return;
            }

            if (DedicatedWords.Code(child.Type()) != TokenCode.builtin_type) {
                ErrorHandling.Add("PrintNode", this.position, $"PrintNode should receive only builtin_type");
                return;
            }
        }
    }
}
