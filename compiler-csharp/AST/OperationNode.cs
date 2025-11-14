using Data.Objects;
namespace AST;
public class OperationNode : Node {
	protected TokenCode op_code;
	protected string operation;
	protected int arg_number;

	public TokenCode Code() => this.op_code;
	public string Operation() => this.operation;
	public int ArgNum() => this.arg_number;
	public void ArgNum(int num) => this.arg_number = num;

	public OperationNode(Position pos, TokenCode code, int args, string op) : base(pos) {
		this.op_code = code;
		this.operation = op;
		this.arg_number = args;
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "OperationNode") Console.WriteLine($"OperationNode(childs={this.childs.Count}, operation_code={this.op_code}, operation={this.operation}, pos=({this.position.Row()}, {this.position.Col()})");
		base.PrintInfo(indent);
	}

	public void AddArgument(Node arg) =>
		this.childs.Add(arg);


	public override void Parse(ref Queue<Token> tokenQueue) { }
}
