using Data.Objects;
namespace AST {
public class RecordNode : Node {
	public RecordNode(Position pos) : base(pos) { }

	public override void Parse(ref Queue<Token> tokenQueue) {
		Token token = tokenQueue.Dequeue();
		while(token.Code() != TokenCode.end_of_body) {
			if(token.Code() != TokenCode.variable_declaration) {
				HandleUnexpectedToken(ref tokenQueue);
				return;
			}
			VarNode field = new VarNode(token.Position());
			field.Parse(ref tokenQueue);
			this.childs.Add(field);
			token = tokenQueue.Dequeue();
		}
	}
}
}
