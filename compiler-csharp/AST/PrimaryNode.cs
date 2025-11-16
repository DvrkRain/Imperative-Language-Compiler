using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;
namespace AST;
public class PrimaryNode : Node {
	public object value;

	public PrimaryNode(Position pos, object val) : base(pos) =>
		this.value = val;

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
		if(this.value is string id) {
		switch(SymbolTable.FindEntry(id)) {
			case Variable vr:
				switch(vr.Value) {
					case int val:
						this.value = val;
						break;

					case bool val:
						this.value = val;
						break;

					default:
						break;
				}
				break;

			default:
				break;
		}
		}
	}
}
