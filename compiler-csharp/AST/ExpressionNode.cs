using Data.Objects;
// using Data.ErrorHandling;
namespace AST;
public class ExpressionNode : Node {
	// private bool cnst;

	public ExpressionNode(Position pos) : base(pos) { }


	public override void Parse(ref Queue<Token> tokenQueue) {
		Stack<Token> operatorStack = new Stack<Token>();
		Token token;
		bool parsing = true;
		while(tokenQueue.Count() > 0 && parsing) {
			token = tokenQueue.Peek();
			switch(token.Code()) {
				case TokenCode.constant_value:
					tokenQueue.Dequeue();
					this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
					break;

				case TokenCode.identifier:
					tokenQueue.Dequeue();
					if(tokenQueue.Peek().Code() == TokenCode.left_parenthesis)
						operatorStack.Push(token);
					else {
						FieldAccessNode field = new FieldAccessNode(token.Position());
						field.Parse(ref tokenQueue);
						this.childs.Add(field);
					}
					break;

				case TokenCode.left_parenthesis:
					tokenQueue.Dequeue();
					operatorStack.Push(token);
					break;

				case TokenCode.comma:
					tokenQueue.Dequeue();
					while(operatorStack.Peek().Code() != TokenCode.left_parenthesis) {
						token = operatorStack.Pop();
						this.childs.Add(new OperationNode(token.Position(), token.Code(), (string)token.Value()));
					}
					break;

				case TokenCode.right_parenthesis:
					tokenQueue.Dequeue();
					while(operatorStack.Peek().Code() != TokenCode.left_parenthesis) {
						if(operatorStack.Count() == 0) {
							// ErrorHandling.Add($"Mismatched parenthesis at {this.position.Row()},{this.position.Col()}.");
							Console.WriteLine("Mismatched parenthesis");
							return;
						}
						token = operatorStack.Pop();
						this.childs.Add(new OperationNode(token.Position(), token.Code(), (string)token.Value()));
					}
					operatorStack.Pop();
					if(operatorStack.Count() > 0 && operatorStack.Peek().Code() == TokenCode.identifier) {
						token = operatorStack.Pop();
						this.childs.Add(new OperationNode(token.Position(), token.Code(), (string)token.Value()));
					}
					break;

				// Operators
				case TokenCode.logic_op:
					tokenQueue.Dequeue();
					while(operatorStack.Count() > 0
						&& operatorStack.Peek().Code() != TokenCode.left_parenthesis
						&& Precedence.Order(token.Code()) >= Precedence.Order(operatorStack.Peek().Code())) {
						Token temp = operatorStack.Pop();
						this.childs.Add(new OperationNode(temp.Position(), temp.Code(), (string)temp.Value()));
					}
					operatorStack.Push(token);
					break;

				case TokenCode.relation_op:
					tokenQueue.Dequeue();
					while(operatorStack.Count() > 0
						&& operatorStack.Peek().Code() != TokenCode.left_parenthesis
						&& Precedence.Order(token.Code()) >= Precedence.Order(operatorStack.Peek().Code())) {
						Token temp = operatorStack.Pop();
						this.childs.Add(new OperationNode(temp.Position(), temp.Code(), (string)temp.Value()));
					}
					operatorStack.Push(token);
					break;

				case TokenCode.factor_op:
					tokenQueue.Dequeue();
					while(operatorStack.Count() > 0
						&& operatorStack.Peek().Code() != TokenCode.left_parenthesis
						&& Precedence.Order(token.Code()) >= Precedence.Order(operatorStack.Peek().Code())) {
						Token temp = operatorStack.Pop();
						this.childs.Add(new OperationNode(temp.Position(), temp.Code(), (string)temp.Value()));
					}
					operatorStack.Push(token);
					break;

				case TokenCode.term_op:
					tokenQueue.Dequeue();
					while(operatorStack.Count() > 0
						&& operatorStack.Peek().Code() != TokenCode.left_parenthesis
						&& Precedence.Order(token.Code()) >= Precedence.Order(operatorStack.Peek().Code())) {
						Token temp = operatorStack.Pop();
						this.childs.Add(new OperationNode(temp.Position(), temp.Code(), (string)temp.Value()));
					}
					operatorStack.Push(token);
					break;

				case TokenCode.dot:
					tokenQueue.Dequeue();
					operatorStack.Push(token);
					break;

				default:
					parsing = false;
					break;
			}
		}
		while(operatorStack.Count() > 0) {
			if(operatorStack.Peek().Code() == TokenCode.left_parenthesis) {
				// ErrorHandling.Add($"Mismatched parenthesis at {this.position.Row()},{this.position.Col()}.");
				Console.WriteLine("Mismatched parenthesis");
				return;
			}
			token = operatorStack.Pop();
			this.childs.Add(new OperationNode(token.Position(), token.Code(), (string)token.Value()));
		}
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ExpressionNode") Console.WriteLine($"ExpressionNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()})");
		base.PrintInfo(indent);
	}
}
