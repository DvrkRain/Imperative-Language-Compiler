using Data.Objects;
namespace AST {
public class AssignmentNode : Node {

	public AssignmentNode(Position pos, PrimaryNode identifier) : base(pos) =>
		this.childs.Add(identifier);


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Assignment sign
		Token token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.bare_assignment) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}

		// Expression
		ExpressionNode expr = new ExpressionNode(token.Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		// Semicolon
		token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}
	}

	public override void PrintInfo(string indent) {
		Console.WriteLine($"AssignmentNode(childs={this.childs.Count})");
		base.PrintInfo(indent);
	}
}
}
