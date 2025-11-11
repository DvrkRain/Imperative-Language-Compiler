using Data.Objects;
namespace AST;
public class PrimaryNode : Node {
	public object value;

	public PrimaryNode(Position pos, object val) : base(pos) =>
		this.value = val;

	public override void Parse(ref Queue<Token> tokenQueue) { }

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "PrimaryNode") Console.WriteLine($"PrimaryNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}), value={this.value})");
		base.PrintInfo(indent);
	}
}
