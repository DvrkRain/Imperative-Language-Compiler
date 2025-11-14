using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;
namespace AST;
public class ExpressionNode : Node {
	private string _type;

	public string Type() => this._type;

	public ExpressionNode(Position pos) : base(pos) =>
		this._type = "void";

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ExpressionNode") Console.WriteLine($"ExpressionNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()})");
		base.PrintInfo(indent);
	}

	private void ParseOperation(ref Queue<Token> tokenQueue, ref Stack<Token> operatorStack, Token token) {
		tokenQueue.Dequeue();
		while(operatorStack.Count() > 0
			&& operatorStack.Peek().Code() != TokenCode.left_parenthesis
			&& Precedence.Order(token.Code()) >= Precedence.Order(operatorStack.Peek().Code())) {
			Token temp = operatorStack.Pop();
			this.childs.Add(new OperationNode(temp.Position(), temp.Code(), 2, (string)temp.Value()));
		}
		operatorStack.Push(token);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Shunting-yard algorithm
		Stack<Token> operatorStack = new Stack<Token>();
		Token token;
		bool parsing = true;
		int args = 1;
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
						PrimaryNode identifier = new PrimaryNode(token.Position(), token.Value());
						this.childs.Add(identifier);
					}
					break;

				case TokenCode.left_parenthesis:
					tokenQueue.Dequeue();
					operatorStack.Push(token);
					break;

				case TokenCode.comma:
                    if (operatorStack.Count() == 0) return;
					tokenQueue.Dequeue();
					while(operatorStack.Peek().Code() != TokenCode.left_parenthesis) {
						++args;
						token = operatorStack.Pop();
						this.childs.Add(new OperationNode(token.Position(), token.Code(), 2, (string)token.Value()));
					}
					break;

				case TokenCode.right_parenthesis:
					tokenQueue.Dequeue();
					while(operatorStack.Peek().Code() != TokenCode.left_parenthesis) {
						if(operatorStack.Count() == 0) {
							ErrorHandling.MismatchedParenthesis(this.GetType().Name, token.Position());
							return;
						}
						token = operatorStack.Pop();
						this.childs.Add(new OperationNode(token.Position(), token.Code(), 2, (string)token.Value()));
					}
					operatorStack.Pop();
					if(operatorStack.Count() > 0 && operatorStack.Peek().Code() == TokenCode.identifier) {
						token = operatorStack.Pop();
						this.childs.Add(new OperationNode(token.Position(), token.Code(), args, (string)token.Value()));
					}
					args = 1;
					break;

				case TokenCode.left_bracket:
					tokenQueue.Dequeue();
					operatorStack.Push(token);
					break;

				case TokenCode.right_bracket:
					tokenQueue.Dequeue();
					while(operatorStack.Peek().Code() != TokenCode.left_bracket) {
						if(operatorStack.Peek().Code() == TokenCode.left_parenthesis) {
							ErrorHandling.MismatchedParenthesis(this.GetType().Name, token.Position());
							return;
						}
						token = operatorStack.Pop();
						this.childs.Add(new OperationNode(token.Position(), token.Code(), 2, (string)token.Value()));
					}
					token = operatorStack.Pop();
					this.childs.Add(new OperationNode(token.Position(), token.Code(), 2, (string)token.Value()));
					break;

				// Operators
				case TokenCode.logic_op:
					this.ParseOperation(ref tokenQueue, ref operatorStack, token);
					break;

				case TokenCode.relation_op:
					this.ParseOperation(ref tokenQueue, ref operatorStack, token);
					break;

				case TokenCode.factor_op:
					this.ParseOperation(ref tokenQueue, ref operatorStack, token);
					break;

				case TokenCode.term_op:
					this.ParseOperation(ref tokenQueue, ref operatorStack, token);
					break;

				case TokenCode.dot:
					this.ParseOperation(ref tokenQueue, ref operatorStack, token);
					break;

				default:
					parsing = false;
					break;
			}
		}
		while(operatorStack.Count() > 0) {
			if((token = operatorStack.Peek()).Code() == TokenCode.left_parenthesis) {
				ErrorHandling.MismatchedParenthesis(this.GetType().Name, token.Position());
				return;
			}
			token = operatorStack.Pop();
			this.childs.Add(new OperationNode(token.Position(), token.Code(), 2, (string)token.Value()));
		}

		// Parsing evaluation queue to expression AST
		Stack<Node> evaluationStack = new Stack<Node>();
		foreach(var child in childs) {
			switch(child) {
				case PrimaryNode:
					evaluationStack.Push(child);
					break;

				case OperationNode oper:
					if(evaluationStack.Count() >= oper.ArgNum()) {
						for(int i=0; i<oper.ArgNum(); i++) {
							oper.AddArgument(evaluationStack.Pop());
						}
					} else if (evaluationStack.Count() == 0) {
						ErrorHandling.Add(this.GetType().Name, oper.Position(), $"Operation {oper.Operation()} does not have enough arguments.");
					} else if (oper.Code() == TokenCode.term_op) {
						oper.AddArgument(evaluationStack.Pop());
					} else if (oper.Code() == TokenCode.logic_op && oper.Operation() == "not") {
						oper.AddArgument(evaluationStack.Pop());
					}
					evaluationStack.Push(oper);
					break;

				default:
					return;
			}
		}
	}


	public override void Verify(ref SymbolTable symTab) {
		base.Verify(ref symTab);

	}
}
