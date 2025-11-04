using Data.Objects;
namespace AST {
public class ForNode : Node {
	public ForNode(Position pos) : base(pos) { }

	public override void Parse(ref Queue<Token> tokenQueue) {
		// Iterator identifier
		Token token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}
		PrimaryNode id = new PrimaryNode(token.Position(), token.Value());

		// 'in' keyword
		token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.in_statement) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}

		// First expression
		ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);
		
		// Second expression (optional)
		token = tokenQueue.Dequeue();
		if(token.Code() == TokenCode.range_sign) {
			token = tokenQueue.Peek();
			expr = new ExpressionNode(token.Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Dequeue();
		}

		// 'loop' keyword
		if(token.Code() != TokenCode.loop_start) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}

		// Loop body
		ProgramNode nested = new ProgramNode(token.Position());
		nested.Parse(ref tokenQueue);
		this.childs.Add(nested);
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ForNode") Console.WriteLine($"ForNode(childs={this.childs.Count})");
		base.PrintInfo(indent);
	}
}
}
