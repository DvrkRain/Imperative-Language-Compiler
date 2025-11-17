using Data.ErrorHandling;
using Data.Objects;
namespace AST;
public class ForNode : Node {
	protected bool reversed;
	public ForNode(Position pos) : base(pos) => this.reversed = false;

	public override void Parse(ref Queue<Token> tokenQueue) {
		// Iterator identifier
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();
		PrimaryNode id = new PrimaryNode(token.Position(), token.Value());
        this.childs.Add(id);

		// 'in' keyword
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.in_statement) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// First expression
		ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);
		
		// Second expression (optional)
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.range_sign) {
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
			expr = new ExpressionNode(token.Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Peek();
		}

		// Reverse (optional)
		if(token.Code() == TokenCode.reverse_statement) {
			this.reversed = true;
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
		}
		// 'loop' keyword
		if(token.Code() != TokenCode.loop_start) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Loop body
		ProgramNode nested = new ProgramNode(token.Position());
		nested.Parse(ref tokenQueue);
		this.childs.Add(nested);
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ForNode") Console.WriteLine($"ForNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}

    public override void Verify() {
        base.Verify();

        ExpressionNode firstExpression = (ExpressionNode)this.childs[1];

        if (firstExpression.Type() != "integer") {
            ErrorHandling.Add("ForNode", this.position, "ForNode should iterate on integer values");
            return;
        }

        if (this.childs[2] is ExpressionNode secondExpression && secondExpression.Type() != "integer") {
            ErrorHandling.Add("ForNode", this.position, "ForNode should iterate on integer values");
            return;
        }
    }
}
