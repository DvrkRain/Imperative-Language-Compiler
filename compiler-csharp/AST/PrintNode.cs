using Data.Objects;
namespace AST {
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
}
}
