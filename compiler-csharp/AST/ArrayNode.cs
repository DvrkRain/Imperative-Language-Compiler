using Data.Objects;
namespace AST {
public class ArrayNode : Node {
	public ArrayNode(Position pos) : base(pos) {}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Left bracket
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.left_bracket) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Expression(optional) and right bracket
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.right_bracket) {
			ExpressionNode expr = new ExpressionNode(token.Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Dequeue();
			if(token.Code() != TokenCode.right_bracket) {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
		} else tokenQueue.Dequeue();

		// Typename
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.identifier || token.Code() == TokenCode.builtin_type) {
			this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
			tokenQueue.Dequeue();
		} else {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
	}

        public override void PrintInfo(string indent) {
			if (this.GetType().Name == "ArrayNode") Console.WriteLine($"ArrayNode(childs={this.childs.Count})");
            base.PrintInfo(indent);
        }
}
}
