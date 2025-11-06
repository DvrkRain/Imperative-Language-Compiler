using Data.Objects;
namespace AST {
public class ForNode : Node {
	protected bool reversed;
	public ForNode(Position pos) : base(pos) => this.reversed = false;

	public override void Parse(ref Queue<Token> tokenQueue) {
		// Iterator identifier
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();
		PrimaryNode id = new PrimaryNode(token.Position(), token.Value());

		// 'in' keyword
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.in_statement) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// First expression
		ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);
		
		// Second expression (optional)
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.range_sign) {
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
			expr = new ExpressionNode(token.Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Dequeue();
		}

		// Reverse (optional)
		if(token.Code() == TokenCode.reverse_statement) {
			this.reversed = true;
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
		}
		// 'loop' keyword
		if(token.Code() != TokenCode.loop_start) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
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
