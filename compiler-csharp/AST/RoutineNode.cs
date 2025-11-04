using Data.Objects;
namespace AST {
public class RoutineNode : Node {
	PrimaryNode type;
	public RoutineNode(Position pos) : base(pos) =>
		this.type = new PrimaryNode(new Position(0,0), "");


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Routine identifier
		Token token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}
		this.childs.Add(new PrimaryNode(token.Position(), token.Value()));

		// Left Parenthesis
		token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.left_parenthesis) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}

		// Parsing parameters
		token = tokenQueue.Peek();
		while(true) {
			ParameterNode param = new ParameterNode(token.Position());
			param.Parse(ref tokenQueue);

			token = tokenQueue.Dequeue();
			if(token.Code() == TokenCode.right_parenthesis)
				break;
			else if(token.Code() == TokenCode.comma) 
				continue;
			else {
				HandleUnexpectedToken(ref tokenQueue);
				return;
			}
		}

		// Explicit type assignment (optional)
		token = tokenQueue.Dequeue();
		if(token.Code() == TokenCode.type_assignment) {
			token = tokenQueue.Dequeue();
			if(token.Code() == TokenCode.identifier || token.Code() == TokenCode.builtin_type)
				this.type = new PrimaryNode(token.Position(), token.Value());
			token = tokenQueue.Dequeue();
		}

		// Body, one-line expression or semicolon for forward declaration
		switch(token.Code()) {
			case TokenCode.semicolon:
				return;

			case TokenCode.is_assignment:
				ProgramNode body = new ProgramNode(tokenQueue.Peek().Position());
				body.Parse(ref tokenQueue);
				this.childs.Add(body);
				break;

			case TokenCode.one_line_body:
				ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
				expr.Parse(ref tokenQueue);
				this.childs.Add(expr);
				break;

			default:
				HandleUnexpectedToken(ref tokenQueue);
				return;
		}
	}
}
}
