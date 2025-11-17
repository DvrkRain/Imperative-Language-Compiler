using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;
namespace AST;
public class PrimaryNode : Node {
	private bool _inExpression;
	public object value;

	public PrimaryNode(Position pos, object val, bool inExpr = false) : base(pos) {
		this.value = val;
		this._inExpression = inExpr;
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "PrimaryNode") Console.WriteLine($"PrimaryNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}), value={this.value})");
		base.PrintInfo(indent);
	}

	public void Negate() {
		if(this.value is int val)
			this.value = -1 * val;
		else if(this.value is float fl)
			this.value = -1 * fl;
		else
			ErrorHandling.Add("Unary minus", this.position, "Unary minus expected to apply to numeral values");
	}

	public void Not() {
		if(this.value is bool flag)
			this.value = !flag;
		else
			ErrorHandling.Add("Not operation", this.position, "Logical not expected to apply to boolean values");
	}


	public override void Parse(ref Queue<Token> tokenQueue) { }

	public override void Verify() {
		if(!this._inExpression) return;
		switch(this.value) {
			case string id:
				switch(SymbolTable.FindEntry(id)) {
					case Variable vr:
						this._type = vr.Type;
						if(vr.Value is not null)
							this.value = vr.Value;
						break;

					default:
						break;
				}
				break;

			case Variable var:
				this._type = var.Type;
				if(var.Value is not null)
					this.value = var.Value;
				break;

			case int val:
				this._type = "integer";
				break;

			case bool val:
				this._type = "boolean";
				break;

			default: break;
		}
	}
}
