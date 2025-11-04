using Data.Objects;
namespace AST {
public class VarNode : Node {
	protected string type;
	protected bool explicit_type;

	public VarNode(Position pos) : base(pos) {
		this.explicit_type = false;
		this.type = "void";
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Search for identifier
		Token token = tokenQueue.Dequeue();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue);
			return;
		}
		this.childs.Add(new PrimaryNode(token.Position(), token.Value()));

		// Check if type is explicitly stated
		bool flag = false;
		token = tokenQueue.Dequeue();
		if(token.Code() == TokenCode.type_assignment) {
			flag = true;
			this.explicit_type = true;
			token = tokenQueue.Dequeue();
			if(token.Code() == TokenCode.builtin_type || token.Code() == TokenCode.identifier)
				this.type = (string)token.Value();
			else {
				HandleUnexpectedToken(ref tokenQueue);
				return;
			}
			token = tokenQueue.Dequeue();
			if(token.Code() == TokenCode.semicolon) return;
		}

		// Check if variable initialized in declaration
		if(token.Code() == TokenCode.is_assignment) {
			ExpressionNode expr = new ExpressionNode(token.Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
		} else if (!flag) {
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
		if (this.GetType().Name == "VarNode") Console.WriteLine($"VarNode(childs={this.childs.Count}, type={this.type}, explicit={this.explicit_type})");
		base.PrintInfo(indent);
	}
}
}
