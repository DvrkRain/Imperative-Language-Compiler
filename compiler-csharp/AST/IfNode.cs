using Data.ErrorHandling;
using Data.Objects;
namespace AST;
public class IfNode : Node {
	public IfNode(Position pos) : base(pos) { }


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Condition
		Token token;
		ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		// 'then' keyword
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.then_statement) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Then branch body
		ProgramNode branch = new ProgramNode(tokenQueue.Peek().Position());
		branch.Parse(ref tokenQueue);
		this.childs.Add(branch);

		// 'else' keyword and respective body (optional)
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.else_statement) {
			tokenQueue.Dequeue();
			branch = new ProgramNode(tokenQueue.Peek().Position());
			branch.Parse(ref tokenQueue);
			this.childs.Add(branch);
		}
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "IfNode") Console.WriteLine($"IfNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}

    public override void Verify() {
        base.Verify();

        ExpressionNode expresison = (ExpressionNode)this.childs[0];
        if (expresison.Type() != "boolean") {
            ErrorHandling.Add("IfNode", this.position, $"IfNode should have a boolean expression");
            return;
        }
    }
}
