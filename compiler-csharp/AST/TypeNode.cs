using Data.Objects;

namespace AST {
public class TypeNode : Node {
	
	public TypeNode(Position pos) : base(pos) { }

	public override void Parse(ref Queue<Token> tokenQueue) {
		// Identifier
		Token token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}
		this.childs.Add(new PrimaryNode(token.Position(), token.Value()));

		// 'is' keyword
		token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.is_assignment) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}

		// Type
		token = tokenQueue.Dequeue();
		switch(token.Code()) {
			case TokenCode.identifier:
				this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
				break;

			case TokenCode.builtin_type:
				this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
				break;

			case TokenCode.record_declaration:
				RecordNode rec = new RecordNode(token.Position());
				rec.Parse(ref tokenQueue);
				this.childs.Add(rec);
				break;

			case TokenCode.array_declaration:
				ArrayNode arr = new ArrayNode(this.position);
				arr.Parse(ref tokenQueue);
				this.childs.Add(arr);
				break;

			default:
				HandleUnexpectedToken(ref tokenQueue);
				return;
		}

		// Check if declaration ends with ';'
		token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue);
		}
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "TypeNode") Console.WriteLine($"TypeNode(childs={this.childs.Count})");
		base.PrintInfo(indent);
	}
}
}
