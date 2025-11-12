using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;
namespace AST;
public class AssignmentNode : Node {

	public AssignmentNode(Position pos, FieldAccessNode identifier) : base(pos) =>
		this.childs.Add(identifier);

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "AssignmentNode") Console.WriteLine($"AssignmentNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


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

	public override void Verify(ref SymbolTable symTab) {
		base.Verify(ref symTab);

		if(this.childs[0].Type() != this.childs[1].Type()) 
			ErrorHandling.Add("AssignmentNode", this.position, $"Trying to put value of type {this.childs[1].Type()} to variable of type {this.childs[0].Type()}.");
	}
}
