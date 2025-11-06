using Data.Objects;
namespace AST {
public class AssignmentNode : Node {

	public AssignmentNode(Position pos, Node identifier) : base(pos) =>
		this.childs.Add(identifier);


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Expression
		Token token = tokenQueue.Peek();
		ExpressionNode expr = new ExpressionNode(token.Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		// Semicolon
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "AssignmentNode") Console.WriteLine($"AssignmentNode(childs={this.childs.Count})");
		base.PrintInfo(indent);
	}
}
}
