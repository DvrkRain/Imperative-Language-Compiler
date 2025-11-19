using Data.Objects;
using SemanticAnalyzer.SymbolTable;
namespace AST;
public class FieldAccessNode : Node {
	private int depth;
	public FieldAccessNode(Position pos) : base(pos) => this.depth = 0;
	public FieldAccessNode(Position pos, Node init) : this(pos) =>
		this.childs.Add(init);

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "FieldAccessNode") Console.WriteLine($"FieldAccessNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		Token token = tokenQueue.Peek();
		while(token.Code() == TokenCode.dot) {
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
			if(token.Code() != TokenCode.identifier) {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
			tokenQueue.Dequeue();
			this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
			token = tokenQueue.Peek();
			this.depth++;
		}
		while(token.Code() == TokenCode.left_bracket) {
			tokenQueue.Dequeue();
			ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position(), true);
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Peek();
			if(token.Code() != TokenCode.right_bracket) {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
		}
	}
}
