using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class ArrayNode : Node {
	public ArrayNode(Position pos) : base(pos) {}

	public override void PrintInfo(string indent) {
		Console.WriteLine($"ArrayNode(childs={this.childs.Count}, pos={this.position.ToString()})");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Left bracket
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.left_bracket) {
			HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "left bracket");
			return;
		}
		tokenQueue.Dequeue();

		// Expression(optional) and right bracket
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.right_bracket) {
			ExpressionNode expr = new ExpressionNode(token.Position(), true);
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Dequeue();
			if(token.Code() != TokenCode.right_bracket) {
				HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "right bracket");
				return;
			}
		} else tokenQueue.Dequeue();

		// Typename
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.identifier || token.Code() == TokenCode.builtin_type) {
			this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
			tokenQueue.Dequeue();
		} else {
			HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "type identifier");
			return;
		}
	}

	
	public override void Verify() {
		base.Verify();

		int current_child_index = 0;
		if(this.childs[current_child_index] is ExpressionNode expr) {
			if(expr.Type() != "integer") {
				ErrorHandling.Add("ArrayNode", this.childs[current_child_index].Position(), "Expression representing array size should be of type integer.");
			}
			current_child_index = 1;
		}

		if(this.childs[current_child_index] is PrimaryNode identifier) {
			if(SymbolTable.FindEntry((string)identifier.value) is not SemanticAnalyzer.SymbolTable.Type)
				ErrorHandling.Add("ArrayNode", this.childs[current_child_index].Position(), "Expected type identifier.");
			this._type = identifier.Name();
		} else
			ErrorHandling.Add("ArrayNode", this.position, $"Expected the PrimaryNode, got {this.childs[current_child_index].GetType().Name}.");
	}
}
