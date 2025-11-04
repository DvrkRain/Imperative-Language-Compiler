using Data.Objects;
namespace AST {
public class ArrayNode : Node {
	public ArrayNode(Position pos) : base(pos) {}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Left bracket
		Token token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.left_bracket) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}

		// Expression(optional) and right bracket
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.right_bracket) {
			ExpressionNode expr = new ExpressionNode(token.Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Dequeue();
			if(token.Code() != TokenCode.right_bracket) {
				HandleUnexpectedToken(ref tokenQueue);
				return;
			}
		}

		// Typename
		token = tokenQueue.Dequeue();
		if(token.Code() == TokenCode.identifier || token.Code() == TokenCode.builtin_type) 
			this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
		else {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}
	}
}
}
