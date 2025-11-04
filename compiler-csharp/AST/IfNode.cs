using Data.Objects;

namespace AST {
public class IfNode : Node {
	public IfNode(Position pos) : base(pos) { }


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Condition
		Token token;
		ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		// 'then' keyword
		token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.then_statement) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}

		// Then branch body
		ProgramNode branch = new ProgramNode(tokenQueue.Peek().Position());
		branch.Parse(ref tokenQueue);
		this.childs.Add(branch);

		// 'else' keyword and respective body (optional)
		token = tokenQueue.Dequeue();
		if(token.Code() == TokenCode.else_statement) {
			branch = new ProgramNode(tokenQueue.Peek().Position());
			branch.Parse(ref tokenQueue);
			this.childs.Add(branch);
		}
	}
}
}
