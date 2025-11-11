using Data.Objects;
namespace AST;
public class OperationNode : Node {
	protected TokenCode op_code;
	protected string operation;

	public OperationNode(Position pos, TokenCode code, string op) : base(pos) {
		this.op_code = code;
		this.operation = op;
	}

	public override void Parse(ref Queue<Token> tokenQueue) { }

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "OperationNode") Console.WriteLine($"OperationNode(childs={this.childs.Count}, operation_code=({this.op_code}), operation=({this.operation}, pos=({this.position.Row()}, {this.position.Col()})");
		base.PrintInfo(indent);
	}
}
