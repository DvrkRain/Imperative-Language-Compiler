using Data.Objects;
namespace AST {
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
}
}
