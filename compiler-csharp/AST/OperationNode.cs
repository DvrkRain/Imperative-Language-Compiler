using Data.Objects; using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;
using System.Reflection;
using System.Reflection.Emit;
using CodeGen;
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
		if(this.arg_number == 0)
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
			if(this.childs[i] is PrimaryNode)
				this.childs[i].Verify();
		}

		bool flag = false;
		PrimaryNode prime;

		switch(this.op_code) {
		// If this operation is routine call
		case TokenCode.identifier:
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

		// If this operation is array index dereferencing
		case TokenCode.left_bracket:
			if (SymbolTable.FindEntry(this.childs[0].Type()) is SemanticAnalyzer.SymbolTable.Type atype
					&& atype.BaseType == "array") {
				if(this.childs[1].Type() != "integer")
					ErrorHandling.Add("Array dereferencing", this.position, "Array index expected to be of integer type");
				if(atype.TypeScope != null) {
					this._type = (string)((Variable)atype.TypeScope.LookupEntry("type")).Value;
					if(this._type == null) this._type = "void";
				}
			} else ErrorHandling.Add("Array dereferencing", this.position,
					"Trying to access non-array variable with indexing.");
			flag = true;
			break;

		// If this operation is record field access (or real number)
		case TokenCode.dot:
			if(this.childs[0].Type() == "integer" && this.childs[1].Type() == "integer") {
				this._type = "real";
			} else if (SymbolTable.FindEntry(this.childs[0].Type()) is SemanticAnalyzer.SymbolTable.Type rtype
					&& rtype.BaseType == "record"
					&& this.childs[1] is PrimaryNode fieldName
					&& fieldName.value is string id) {
				flag = true;
				if(rtype.TypeScope is Scope strct) {
					if(strct.LookupEntry(id) is Variable var) {
						this._type = var.Type;
						flag = true;
					} else if(strct.LookupEntry(id) is null)
						ErrorHandling.Add("OperationNode", this.position, $"Record does not have a field named {id}.");
					else ErrorHandling.Add("OperationNode", this.position, "Record cannot contain non-variable fields");
				} else ErrorHandling.Add("OperationNode", this.position, "Record is not properly changed or not contain scope.");
			} else if (SymbolTable.FindEntry(this.childs[0].Type()) is SemanticAnalyzer.SymbolTable.Type type
					&& type.BaseType == "array"
					&& this.childs[1] is PrimaryNode identifier
					&& (string)identifier.value == "size") {
				this._type = "integer";
			} else ErrorHandling.Add("OperationNode", this.position, "Unexpected arguments on dot operation");
			break;
			
		case TokenCode.logic_op:
			this._type = "void";
			if(this.childs[0].Type() != "boolean") {
				ErrorHandling.Add("OperationNode", this.position, "Non-boolean operands on logic operation");
				return;
			}
			if(this.arg_number == 2 && this.childs[1].Type() != "boolean") {
				ErrorHandling.Add("OperationNode", this.position, "Non-boolean operands on logic operation");
				return;
			}
			this._type = "boolean";
			break;

		case TokenCode.relation_op:
			this._type = "void";
			if(		this.childs[0].Type() == "boolean"
					&& this.childs[1].Type() != "boolean"
					|| this.childs[1].Type() == "boolean"
					&& this.childs[0].Type() != "boolean") {
				ErrorHandling.Add("OperationNode", this.position, "Cannot compare numeric and boolean");
				return;
			}
			this._type = "boolean";
			break;
			
		case TokenCode.factor_op:
			this._type = "void";
			if(this.childs[0].Type() == "boolean") {
				ErrorHandling.Add("OperationNode", this.position, "Boolean operands on non-boolean operation");
				return;
			}
			if(this.childs[1].Type() == "boolean") {
				ErrorHandling.Add("OperationNode", this.position, "Boolean operands on non-boolean operation");
				return;
			}
			this._type = "integer";
			if(this.childs[0].Type() == "real") this._type = "real";
			if(this.childs[1].Type() == "real") this._type = "real";
			break;

		case TokenCode.term_op:
			this._type = "void";
			if(this.childs[0].Type() == "boolean") {
				ErrorHandling.Add("OperationNode", this.position, "Boolean operands on non-boolean operation");
				return;
			}
			if(this.arg_number == 2 && this.childs[1].Type() == "boolean") {
				ErrorHandling.Add("OperationNode", this.position, "Boolean operands on non-boolean operation");
				return;
			}
			this._type = "integer";
			if(this.childs[0].Type() == "real") this._type = "real";
			if(this.arg_number == 2 && this.childs[1].Type() == "real") this._type = "real";
			break;

		default:
			ErrorHandling.Add("OperationNode", this.position, "Unknown operation");
			break;
		}

		// Checking if current operation calculatable at compile time
		foreach(var child in childs) {
			if(child is PrimaryNode prim) {
				if((prim.value is string)
					|| prim.value is null
					|| prim.value is ExpressionNode)
					flag = true;
			} else flag = true;
		}
		if(flag) return;

		switch(this._operation) {
			// term operations
			case "+":
				if(this.arg_number != 1) {
					if(((PrimaryNode)this.childs[0]).value is int) {
						int i1 = (int)((PrimaryNode)this.childs[0]).value;
						if(((PrimaryNode)this.childs[1]).value is int i2) {
							this.childs[0] = new PrimaryNode(this.position, i1+i2, true);
						} else {
							float f2 = (float)((PrimaryNode)this.childs[1]).value;
							this.childs[0] = new PrimaryNode(this.position, i1+f2, true);
						}
					} else {
						float f1 = (float)((PrimaryNode)this.childs[0]).value;
						if(((PrimaryNode)this.childs[1]).value is int i2) {
							this.childs[0] = new PrimaryNode(this.position, f1+i2, true);
						} else {
							float f2 = (float)((PrimaryNode)this.childs[1]).value;
							this.childs[0] = new PrimaryNode(this.position, f1+f2, true);
						}
					}
				}
				break;

			case "-":
				if(this.arg_number != 1) {
					if(((PrimaryNode)this.childs[0]).value is int) {
						int i1 = (int)((PrimaryNode)this.childs[0]).value;
						if(((PrimaryNode)this.childs[1]).value is int i2) {
							this.childs[0] = new PrimaryNode(this.position, i1-i2, true);
						} else {
							float f2 = (float)((PrimaryNode)this.childs[1]).value;
							this.childs[0] = new PrimaryNode(this.position, i1-f2, true);
						}
					} else {
						float f1 = (float)((PrimaryNode)this.childs[0]).value;
						if(((PrimaryNode)this.childs[1]).value is int i2) {
							this.childs[0] = new PrimaryNode(this.position, f1-i2, true);
						} else {
							float f2 = (float)((PrimaryNode)this.childs[1]).value;
							this.childs[0] = new PrimaryNode(this.position, f1-f2, true);
						}
					}
				} else {
					if(((PrimaryNode)this.childs[0]).value is int) {
						int i1 = (int)((PrimaryNode)this.childs[0]).value;
						this.childs[0] = new PrimaryNode(this.position, -1*i1, true);
					} else {
						float f1 = (float)((PrimaryNode)this.childs[0]).value;
						this.childs[0] = new PrimaryNode(this.position, -1*f1, true);
					}
				}
				break;

			// Factor operations
			case "*":
				if(((PrimaryNode)this.childs[0]).value is int) {
					int i1 = (int)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, i1*i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, i1*f2, true);
					}
				} else {
					float f1 = (float)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, f1*i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, f1*f2, true);
					}
				}
				break;

			case "/":
				if(((PrimaryNode)this.childs[0]).value is int) {
					int i1 = (int)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, i1/i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, i1/f2, true);
					}
				} else {
					float f1 = (float)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, f1/i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, f1/f2, true);
					}
				}
				break;

			case "%":
				if(((PrimaryNode)this.childs[0]).value is int) {
					int i1 = (int)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, i1%i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, i1%f2, true);
					}
				} else {
					float f1 = (float)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, f1%i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, f1%f2, true);
					}
				}
				break;

			// Relation operations
			case "<":
				if(((PrimaryNode)this.childs[0]).value is int) {
					int i1 = (int)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, i1<i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, i1<f2, true);
					}
				} else if(((PrimaryNode)this.childs[0]).value is float) {
					float f1 = (float)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, f1<i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, f1<f2, true);
					}
				} else {
					bool b1 = (bool)((PrimaryNode)this.childs[0]).value;
					bool b2 = (bool)((PrimaryNode)this.childs[1]).value;
					this.childs[0] = new PrimaryNode(this.position, !b1&&b2, true);
				}
				break;

			case "<=":
				if(((PrimaryNode)this.childs[0]).value is int) {
					int i1 = (int)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, i1<=i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, i1<=f2, true);
					}
				} else if(((PrimaryNode)this.childs[0]).value is float) {
					float f1 = (float)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, f1<=i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, f1<=f2, true);
					}
				} else {
					bool b1 = (bool)((PrimaryNode)this.childs[0]).value;
					bool b2 = (bool)((PrimaryNode)this.childs[1]).value;
					this.childs[0] = new PrimaryNode(this.position, !b1||b2, true);
				}
				break;

			case "/=":
				if(((PrimaryNode)this.childs[0]).value is int) {
					int i1 = (int)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, i1!=i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, i1!=f2, true);
					}
				} else if(((PrimaryNode)this.childs[0]).value is float) {
					float f1 = (float)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, f1!=i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, f1!=f2, true);
					}
				} else {
					bool b1 = (bool)((PrimaryNode)this.childs[0]).value;
					bool b2 = (bool)((PrimaryNode)this.childs[1]).value;
					this.childs[0] = new PrimaryNode(this.position, b1!=b2, true);
				}
				break;

			case "==":
				if(((PrimaryNode)this.childs[0]).value is int) {
					int i1 = (int)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, i1==i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, i1==f2, true);
					}
				} else if(((PrimaryNode)this.childs[0]).value is float) {
					float f1 = (float)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, f1==i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, f1==f2, true);
					}
				} else {
					bool b1 = (bool)((PrimaryNode)this.childs[0]).value;
					bool b2 = (bool)((PrimaryNode)this.childs[1]).value;
					this.childs[0] = new PrimaryNode(this.position, b1==b2, true);
				}
				break;

			case ">=":
				if(((PrimaryNode)this.childs[0]).value is int) {
					int i1 = (int)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, i1>=i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, i1>=f2, true);
					}
				} else if(((PrimaryNode)this.childs[0]).value is float) {
					float f1 = (float)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, f1>=i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, f1>=f2, true);
					}
				} else {
					bool b1 = (bool)((PrimaryNode)this.childs[0]).value;
					bool b2 = (bool)((PrimaryNode)this.childs[1]).value;
					this.childs[0] = new PrimaryNode(this.position, b1||!b2, true);
				}
				break;

			case ">":
				if(((PrimaryNode)this.childs[0]).value is int) {
					int i1 = (int)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, i1>i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, i1>f2, true);
					}
				} else if(((PrimaryNode)this.childs[0]).value is float) {
					float f1 = (float)((PrimaryNode)this.childs[0]).value;
					if(((PrimaryNode)this.childs[1]).value is int i2) {
						this.childs[0] = new PrimaryNode(this.position, f1>i2, true);
					} else {
						float f2 = (float)((PrimaryNode)this.childs[1]).value;
						this.childs[0] = new PrimaryNode(this.position, f1>f2, true);
					}
				} else {
					bool b1 = (bool)((PrimaryNode)this.childs[0]).value;
					bool b2 = (bool)((PrimaryNode)this.childs[1]).value;
					this.childs[0] = new PrimaryNode(this.position, b1&&!b2, true);
				}
				break;

			// Logic operations
			case "not":
				bool l1 = (bool)((PrimaryNode)this.childs[0]).value;
				this.childs[0] = new PrimaryNode(this.position, !l1, true);
				break;

			case "or":
				l1 = (bool)((PrimaryNode)this.childs[0]).value;
				bool l2 = (bool)((PrimaryNode)this.childs[0]).value;
				this.childs[0] = new PrimaryNode(this.position, l1||l2, true);
				break;

			case "and":
				l1 = (bool)((PrimaryNode)this.childs[0]).value;
				l2 = (bool)((PrimaryNode)this.childs[0]).value;
				this.childs[0] = new PrimaryNode(this.position, l1&&l2, true);
				break;

			case "xor":
				l1 = (bool)((PrimaryNode)this.childs[0]).value;
				l2 = (bool)((PrimaryNode)this.childs[0]).value;
				this.childs[0] = new PrimaryNode(this.position, l1^l2, true);
				break;

			case ".":
				if(((PrimaryNode)this.childs[0]).value is int) {
					string s1 = (string)((PrimaryNode)this.childs[0]).value;
					string s2 = (string)((PrimaryNode)this.childs[1]).value;
					string res = $"{s1},{s2}";
					prime = new PrimaryNode(this.position, float.Parse(res), true);
					prime.Type("real");
					this.childs[0] = prime;
				} else if(((PrimaryNode)this.childs[0]).value is Scope structure) {
					if(((PrimaryNode)this.childs[1]).value is string id) {
					switch(structure.LookupEntry(id)) {
						case Variable var:
							this._type = var.Type;
							prime = new PrimaryNode(this.position, var, true);
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
				// } else if(this.childs[0].Type() == "array") {
				// 	if(((PrimaryNode)this.childs[1]).value is string id && id == "size") {
				// 		this._type = "integer";
				// 		this.childs[0] = new PrimaryNode(this.position, );
					// } else ErrorHandling.Add("Array dereferencing", this.position, "Array only have 'size' field");
				} else ErrorHandling.Add("OperationNode", this.position, "Unexpected arguments on dot operation");
				break;

			default:
				break;
		}
		this.arg_number = 0;
	}


	public override void Generate(CodeGenContext ctx) {
		if(this._operation == ".")
			this.childs[0].Generate(ctx);
		else
			base.Generate(ctx);

		switch(this._operation) {
			// Term operations
			case "+":
				if(this.arg_number == 2)
					ctx.CurrentIL.Emit(OpCodes.Add);
				break;

			case "-":
				if(this.arg_number == 2)
					ctx.CurrentIL.Emit(OpCodes.Sub);
				else
					ctx.CurrentIL.Emit(OpCodes.Neg);
				break;

			// Factor operations
			case "*":
				ctx.CurrentIL.Emit(OpCodes.Mul);
				break;

			case "/":
				ctx.CurrentIL.Emit(OpCodes.Div);
				break;

			case "%":
				ctx.CurrentIL.Emit(OpCodes.Rem);
				break;

			// Relation operation
			case "/=":
				ctx.CurrentIL.Emit(OpCodes.Ceq);
				ctx.CurrentIL.Emit(OpCodes.Neg);
				break;

			case "<":
				ctx.CurrentIL.Emit(OpCodes.Clt);
				break;

			case "<=":
				ctx.CurrentIL.Emit(OpCodes.Cgt);
				ctx.CurrentIL.Emit(OpCodes.Neg);
				break;

			case "==":
				ctx.CurrentIL.Emit(OpCodes.Ceq);
				break;

			case ">=":
				ctx.CurrentIL.Emit(OpCodes.Clt);
				ctx.CurrentIL.Emit(OpCodes.Neg);
				break;

			case ">":
				ctx.CurrentIL.Emit(OpCodes.Cgt);
				break;

			// Logic operation
			case "not":
				ctx.CurrentIL.Emit(OpCodes.Neg);
				break;

			case "and":
				ctx.CurrentIL.Emit(OpCodes.And);
				break;

			case "or":
				ctx.CurrentIL.Emit(OpCodes.Or);
				break;

			case "xor":
				ctx.CurrentIL.Emit(OpCodes.Xor);
				break;

			case ".":
				var currentType = ctx.ResolveType(this.childs[0].Type());
				var fieldInfo = currentType.GetField(((PrimaryNode)this.childs[1]).Name());
				ctx.CurrentIL.Emit(OpCodes.Ldfld, fieldInfo);
				break;

			case "[":
				currentType = ctx.ResolveType(this._type);
				ctx.CurrentIL.Emit(OpCodes.Ldelem, currentType);
				break;

			case string id:
				MethodInfo method = ctx.Methods[id];
				ctx.CurrentIL.Emit(OpCodes.Call, method);
				break;

			default:
				break;
		}
	}
}
