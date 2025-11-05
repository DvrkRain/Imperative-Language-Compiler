using Data.Objects;
namespace AST {
public class ParameterNode : Node {
	public ParameterNode(Position pos) : base(pos) { }
	

	public override void Parse(ref Queue<Token> tokenQueue) {
		// Identifier
		Token token = tokenQueue.Peek();
		if(token.Code() == TokenCode.identifier) {
			this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
		} else {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Type assignment
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.type_assignment) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Type
		token = tokenQueue.Dequeue();
		if(token.Code() == TokenCode.identifier || token.Code() == TokenCode.builtin_type)
			this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
	}
}
}
