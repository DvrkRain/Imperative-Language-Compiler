using Data.Objects;
namespace AST {
public class FieldAccessNode : Node {
	protected bool call;

	public FieldAccessNode(Position pos) : base(pos) => this.call = false;
	public FieldAccessNode(Position pos, Node init) : this(pos) =>
		this.childs.Add(init);

	public override void Parse(ref Queue<Token> tokenQueue) {
		Token token = tokenQueue.Peek();
		if(token.Code() == TokenCode.left_parenthesis) {
			this.call = true;
			tokenQueue.Dequeue();
			ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Peek();
			while(token.Code() == TokenCode.comma) {
				tokenQueue.Dequeue();
				expr = new ExpressionNode(token.Position());
				expr.Parse(ref tokenQueue);
				this.childs.Add(expr);
				token = tokenQueue.Peek();
			}

			if(token.Code() != TokenCode.right_parenthesis)
				HandleUnexpectedToken(ref tokenQueue, token.Position());
			tokenQueue.Dequeue();
			return;
		}
		while(token.Code() == TokenCode.dot) {
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
			if(token.Code() != TokenCode.identifier) {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
			tokenQueue.Dequeue();
			this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
			token = tokenQueue.Peek();
		}
		while(token.Code() == TokenCode.left_bracket) {
			tokenQueue.Dequeue();
			ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Peek();
			if(token.Code() != TokenCode.right_bracket) {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
		}
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "FieldAccessNode") Console.WriteLine($"FieldAccessNode(childs={this.childs.Count})");
		base.PrintInfo(indent);
	}
}
}
