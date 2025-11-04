using Data.Objects;
namespace AST {
public class RecordNode : Node {
	public RecordNode(Position pos) : base(pos) { }

	public override void Parse(ref Queue<Token> tokenQueue) {
		Token token = tokenQueue.Peek();
		while(token.Code() != TokenCode.end_of_body) {
			if(token.Code() != TokenCode.variable_declaration) {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
			tokenQueue.Dequeue();
			VarNode field = new VarNode(token.Position());
			field.Parse(ref tokenQueue);
			this.childs.Add(field);
			token = tokenQueue.Peek();
		}
		tokenQueue.Dequeue();
	}

	public override void PrintInfo(string indent) {
		Console.WriteLine($"RecordNode(childs={this.childs.Count})");
		base.PrintInfo(indent);
	}
}
}
