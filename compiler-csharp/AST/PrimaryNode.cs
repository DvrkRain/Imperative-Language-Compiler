using Data.Objects;
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


	public override void Parse(ref Queue<Token> tokenQueue) { }

	public override void Verify() {
		if(!this._inExpression) return;
		switch(this.value) {
			case ExpressionNode expr:
				if(expr.Value() is PrimaryNode prime) {
					this.value = prime.value;
					this._type = prime.Type();
				}
				break;

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
