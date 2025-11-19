using Data.ErrorHandling;
using Data.Objects;
namespace AST;
public class IfNode : Node {
	public IfNode(Position pos) : base(pos) { }

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "IfNode") Console.WriteLine($"IfNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Condition
		Token token;
		ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		// 'then' keyword
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.then_statement) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Then branch body
		ProgramNode branch = new ProgramNode(tokenQueue.Peek().Position());
		branch.Parse(ref tokenQueue);
		this.childs.Add(branch);

		// 'else' keyword and respective body (optional)
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.else_statement) {
			tokenQueue.Dequeue();
			branch = new ProgramNode(tokenQueue.Peek().Position());
			branch.Parse(ref tokenQueue);
			this.childs.Add(branch);
		}
	}


    public override void Verify() {
		this.childs[0].Verify();
		bool ret = false;
		bool check = false;

		if(this.childs.Count == 3 && Returning.Count() > 0 && !Returning.Peek().returned) {
			check = true;
			Returning.Push(ReturningStatus.Copy(Returning.Peek()));
		}
		this.childs[1].Verify();
		if(check) ret = Returning.Pop().returned;

		if(check) Returning.Push(ReturningStatus.Copy(Returning.Peek()));
		if(this.childs.Count == 3) this.childs[2].Verify();
		if(check) ret = ret && Returning.Pop().returned;

		Returning.Push(new ReturningStatus(Returning.Peek().returned || ret, Returning.Pop().ret_type));

        if (this.childs[0].Type() != "boolean") {
            ErrorHandling.Add("IfNode", this.position, $"'if' statement should have a boolean expression");
            return;
        }
    }
}
