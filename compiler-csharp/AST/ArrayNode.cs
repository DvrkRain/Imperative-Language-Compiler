using Data.Objects;
using SemanticAnalyzer.SymbolTable;
namespace AST;
public class ArrayNode : Node {
	public ArrayNode(Position pos) : base(pos) {}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ArrayNode")
			Console.WriteLine($"ArrayNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Left bracket
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.left_bracket) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Expression(optional) and right bracket
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.right_bracket) {
			ExpressionNode expr = new ExpressionNode(token.Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Dequeue();
			if(token.Code() != TokenCode.right_bracket) {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
		} else tokenQueue.Dequeue();

		// Typename
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.identifier || token.Code() == TokenCode.builtin_type) {
			this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
			tokenQueue.Dequeue();
		} else {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
	}

	
	public override void Verify(ref SymbolTable symTab) {
		base.Verify(ref symTab);

		int current_child_index = 0;
		if(this.childs[current_child_index] is ExpressionNode expr) {
			if(expr.Type() != "integer") {
				ErrorHandling.Add($"ArrayNode({this.position.Row()},{this.position.Col()}): Expression representing array size should be of type integer.");
			}
			current_child_index = 1;
		}

		if(this.childs[current_child_index] is PrimaryNode identifier) {
			if(symTab.FindEntry((string)identifier.value) is not SemanticAnalyzer.SymbolTable.Type) {
				ErrorHandling.Add($"ArrayNode({this.position.Row()},{this.position.Col()}): Expected type identifier.");
			}
		} else
			ErrorHandling.Add($"ArrayNode({this.position.Row()},{this.position.Col()}): Expected the PrimaryNode, got {this.childs[current_child_index].GetType().Name}.");
	}
}
