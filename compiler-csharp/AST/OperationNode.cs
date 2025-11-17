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
			if(this.childs[i] is OperationNode oper)
				this.childs[i] = oper.Value();
		}

		bool flag = false;
		PrimaryNode prime;

		// If this operation is routine call
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
							$"Wrong parameter type on {this._operation} node on position {i}: expected type {rout.Parameters[i].Type}, got {this.childs[i].Type()}.");
						flag = true;
					}
				}
				if(flag) return;
				this._type = rout.ReturnType;
				return;

			default:
				ErrorHandling.Add("Routine call", this.position, "Undeclared routine");
				flag = true;
				return;
		}
		}

		// If this operation is array index dereferencing
		else if(this.op_code == TokenCode.left_bracket) {
			if(this.childs[0].Type() == "array") {
				if(this.childs[1].Type() != "integer") {
				} else ErrorHandling.Add("Array dereferencing", this.position, "Array index expected to be of integer type");
			} else ErrorHandling.Add("Array dereferencing", this.position,
					"Trying to access non-array variable with indexing.");
		}

		// If this operation is record field access (or real number)
		else if(this.op_code == TokenCode.dot) {
			if(this.childs[0].Type() == "integer" && this.childs[1].Type() == "integer") {
				this._type = "real";
			} else if (this.childs[0].Type() == "record"
					&& this.childs[1] is PrimaryNode fieldName
					&& fieldName.value is string id) {
				if(this.childs[0] is PrimaryNode rec && rec.value is Scope strct) {
					if(strct.LookupEntry(id) is Variable var) {
						this._type = var.Type;
						prime = new PrimaryNode(this.position, var);
						prime.Verify();
						this.childs[0] = prime;
					} else if(strct.LookupEntry(id) is null)
						ErrorHandling.Add("OperationNode", this.position, $"Record does not have a field named {id}.");
					else ErrorHandling.Add("OperationNode", this.position, "Record cannot contain non-variable fields");
				} else ErrorHandling.Add("OperationNode", this.position, "Record is not properly changed or not contain scope.");
			} else if (this.childs[0].Type() == "array"
					&& this.childs[1] is PrimaryNode identifier
					&& (string)identifier.value == "size") {
				this._type = "integer";
			} else ErrorHandling.Add("OperationNode", this.position, "Unexpected arguments on dot operation");
		}

		// Checking if current operation calculatable at compile time
		foreach(var child in childs) {
			if(child is not PrimaryNode) flag = true;
		}
		// TODO: Finish expression const calc
		flag = true;
		if(flag) return;

		// If this operation is record field access (or real number)
		if(this.op_code == TokenCode.dot) {
			flag = true;
			if(((PrimaryNode)this.childs[0]).value is int i1) {
				this._type = "real";
				if(((PrimaryNode)this.childs[0]).value is int i2) {
					prime = new PrimaryNode(this.position, (float)i1 + (float)i2 / Math.Pow(10, Math.Ceiling(Math.Log(i2, 10))));
					prime.Type("real");
					this.childs[0] = prime;
				} else {
					ErrorHandling.Add("Real number", this.position, "Expected integer value at both parts of real number");
				}
			} else if (((PrimaryNode)this.childs[0]).value is Scope structure) {
				if(((PrimaryNode)this.childs[1]).value is string id) {
				switch(structure.LookupEntry(id)) {
					case Variable var:
						this._type = var.Type;
						prime = new PrimaryNode(this.position, var);
						prime.Verify();
						this.childs[0] = prime;
						break;

					case null:
						ErrorHandling.Add("Record field access", this.position, $"Record does not containt field {id}");
						break;

					default:
						ErrorHandling.Add("Record field access", this.position, $"Record field is not variable");
						break;
				}
				} else ErrorHandling.Add("Record field access", this.position, "Expected identifier as field name");
			} else if(this.childs[0].Type() == "array") {
				if(((PrimaryNode)this.childs[1]).value is string id && id == "size") {
					// Array size
				} else ErrorHandling.Add("Array dereferencing", this.position, "Array only have 'size' field");
			} else ErrorHandling.Add("OperationNode", this.position, "Unexpected arguments on dot operation");
		}

		// Calculating const expression (TODO)
		this.arg_number = 0;
		if(this.arg_number == 1) {
			if(((PrimaryNode)this.childs[0]).value is string) return;
			if(this._operation == "-")
				((PrimaryNode)this.childs[0]).Negate();
			else if(this._operation == "not")
				((PrimaryNode)this.childs[0]).Not();
		} else {
		switch(this._operation) {
			case "+":
				break;

			case "-":
				break;

			default:
				ErrorHandling.Add("OperationNode", this.position, "Unknown operation");
				break;
		}
		}
	}
}
