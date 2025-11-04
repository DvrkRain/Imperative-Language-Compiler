using Data.Objects;
namespace AST {
public class WhileNode : Node {
	public WhileNode(Position pos) : base(pos) { }


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Condition expression
		ExpressionNode expr = new ExpressionNode(this.position);
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		// 'loop' keyword
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.loop_start) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Loop body
		ProgramNode nested = new ProgramNode(tokenQueue.Peek().Position());
		nested.Parse(ref tokenQueue);
		this.childs.Add(nested);
	}

	public override void PrintInfo(string indent) {
		Console.WriteLine($"WhileNode(childs={this.childs.Count})");
		base.PrintInfo(indent);
	}
}
}
