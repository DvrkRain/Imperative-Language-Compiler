using Data.Objects;
namespace AST;
public class RoutineNode : Node {
	public RoutineNode(Position pos) : base(pos) { }


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Routine identifier
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();
		this.childs.Add(new PrimaryNode(token.Position(), token.Value()));

		// Left Parenthesis
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.left_parenthesis) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Parsing parameters
		while(true) {
			ParameterNode param = new ParameterNode(tokenQueue.Peek().Position());
			param.Parse(ref tokenQueue);

			token = tokenQueue.Peek();
			if(token.Code() == TokenCode.right_parenthesis) {
				tokenQueue.Dequeue();
				break;
			} else if(token.Code() == TokenCode.comma) {
				tokenQueue.Dequeue();
				continue;
			} else {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
		}

		// Explicit type assignment (optional)
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.type_assignment) {
			tokenQueue.Dequeue();
			token = tokenQueue.Dequeue();
			if(token.Code() == TokenCode.identifier || token.Code() == TokenCode.builtin_type)
				this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
			token = tokenQueue.Peek();
		}

		// Body, one-line expression or semicolon for forward declaration
		switch(token.Code()) {
			case TokenCode.semicolon:
				tokenQueue.Dequeue();
				return;

			case TokenCode.is_assignment:
				tokenQueue.Dequeue();
				ProgramNode body = new ProgramNode(tokenQueue.Peek().Position());
				body.Parse(ref tokenQueue);
				this.childs.Add(body);
				break;

			case TokenCode.one_line_body:
				tokenQueue.Dequeue();
				ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
				expr.Parse(ref tokenQueue);
				this.childs.Add(expr);
				break;

			default:
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
		}
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "RoutineNode") Console.WriteLine($"RoutineNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()})");
		base.PrintInfo(indent);
	}
}
