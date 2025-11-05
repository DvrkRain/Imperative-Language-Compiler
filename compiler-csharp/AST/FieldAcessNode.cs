using Data.Objects;
namespace AST {
public class FieldAccessNode : Node {
	public FieldAccessNode(Position pos) : base(pos) { }
	public FieldAccessNode(Position pos, Node init) : this(pos) =>
		this.childs.Add(init);

	public override void Parse(ref Queue<Token> tokenQueue) {
		while(true) {
			Token token = tokenQueue.Peek();
			while(token.Code() == TokenCode.dot) {
				tokenQueue.Dequeue();
				token = tokenQueue.Peek();
				if(token.Code() != TokenCode.identifier) {
					HandleUnexpectedToken(ref tokenQueue, token.Position());
					return;
				}
				tokenQueue.Dequeue();
				this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
				token = tokenQueue.Peek();
			}
		}
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "FieldAccessNode") Console.WriteLine($"FieldAccessNode(childs={this.childs.Count})");
		base.PrintInfo(indent);
	}
}
}
