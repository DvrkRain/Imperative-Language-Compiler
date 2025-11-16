using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;
namespace AST;
public class OperationNode : Node {
	protected TokenCode op_code;
	protected bool _constant;
	protected string _operation;
	protected int arg_number;

	public TokenCode Code() => this.op_code;
	public string Operation() => this._operation;
	public bool Constant() => this._constant;
	public int ArgNum() => this.arg_number;
	public void ArgNum(int num) => this.arg_number = num;

	public OperationNode(Position pos, TokenCode code, string op, int args = 2) : base(pos) {
		this._type = "void";
		this.op_code = code;
		this._constant = false;
		this._operation = op;
		this.arg_number = args;
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "OperationNode") Console.WriteLine($"OperationNode(childs={this.childs.Count}, operation_code={this.op_code}, operation={this._operation}, pos={this.position.ToString()}");
		base.PrintInfo(indent);
	}

	public void AddArgument(Node arg) =>
		this.childs.Add(arg);

	public Node Value() {
		if(this.arg_number == 0 && this.childs.Count() == 1)
			return this.childs[0];
		return this;
	}


	public override void Parse(ref Queue<Token> tokenQueue) { }
	public override void Verify() {
		this.childs.Reverse();
		base.Verify();

		for(int i=0; i<this.childs.Count(); i++) {
		if(this.childs[i] is OperationNode oper) {
			this.childs[i] = oper.Value();
		}
		}

		bool flag = false;
		if(this.op_code == TokenCode.identifier) {
		switch(SymbolTable.FindEntry(this._operation)) {
			case Routine rout:
				if(this.arg_number != rout.Parameters.Count()) {
					ErrorHandling.Add("Routine call", this.position, "Wrong number of parameters for routine call");
					return;
				}
				for(int i=0; i<this.arg_number; i++) {
					if(this.childs[i].Type() != rout.Parameters[i].Type) {
						ErrorHandling.Add("Routine call", this.childs[i].Position(),
							$"Wrong parameter type on {this._operation} node on position {i}: expected type {this.childs[i].Type()}, got {rout.Parameters[i].Type}.");
						flag = true;
					}
				}
				if(flag) return;
				this._type = rout.ReturnType;
				return;

			default:
				ErrorHandling.Add("Routine call", this.position, "Undeclared routine");
				return;
		}
		}

		foreach(var child in childs) {
			if(child is not PrimaryNode) flag = true;
		}
		if(flag) return;
		if(this.arg_number == 1) {
			if(this._operation == "-")
				((PrimaryNode)this.childs[0]).Negate();
			else if(this._operation == "not")
				((PrimaryNode)this.childs[0]).Not();
			this.arg_number = 0;
		} else {
		}
	}
}
