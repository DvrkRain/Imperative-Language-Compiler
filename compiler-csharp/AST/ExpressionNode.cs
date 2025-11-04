using Data.Objects;
namespace AST {
public class ExpressionNode : Node {
	private TokenCode priorityCode;
	private string operation;
	private Node left;
	private Node right;
	private bool initialized;
	private bool cnst;

	public ExpressionNode(Position pos) : base(pos) {
		this.initialized = false;
		this.cnst = false;
	}
	public ExpressionNode(Position pos, Node init, string operation, TokenCode priorCode) : base(pos) {
		this.initialized = true;
		this.cnst = false;
		this.left = init;
		this.operation = operation;
	}
	public ExpressionNode(Position pos, Node init, string operation, TokenCode priorCode, Node rightInit)
		: this(pos, init, operation, priorCode) => this.right = rightInit;


	public override void Parse(ref Queue<Token> tokenQueue) {
		int step = 0;
		if(initialized) step = 2;

		bool parenthesised = false;
		while(step < 4) {
			Token token = tokenQueue.Peek();
			// Console.WriteLine($"{token.Code()} at {token.Position().Row()},{token.Position().Col()} on step {step}.");
			switch(step) {
				case 0:
					step = 1;
					switch(token.Code()) {
						case TokenCode.left_parenthesis:
							if(parenthesised) {
								this.left = new ExpressionNode(token.Position());
								this.left.Parse(ref tokenQueue);
							} else {
								parenthesised = true;
								step = 0;
								tokenQueue.Dequeue();
							}
							break;

						case TokenCode.identifier:
							this.left = new PrimaryNode(token.Position(), token.Value());
							tokenQueue.Dequeue();
							token = tokenQueue.Peek();
							if(token.Code() == TokenCode.dot)
								this.left = new FieldAccessNode(token.Position(), this.left);
							break;

						case TokenCode.constant_value:
							this.left = new PrimaryNode(token.Position(), token.Value());
							tokenQueue.Dequeue();
							token = tokenQueue.Peek();
							if(token.Code() == TokenCode.dot)
								this.left = new FieldAccessNode(token.Position(), this.left);
							break;

						case TokenCode.logic_op when (string)token.Value() == "not":
							this.left = new PrimaryNode(token.Position(), 0);
							this.operation = (string)token.Value();
							step = 2;
							tokenQueue.Dequeue();
							break;

						case TokenCode.term_op:
							this.left = new PrimaryNode(token.Position(), 0);
							this.operation = (string)token.Value();
							step = 2;
							tokenQueue.Dequeue();
							break;

						default:
							step = 4;
							HandleUnexpectedToken(ref tokenQueue, token.Position());
							return;
					}
					break;

				case 1:
					step = 2;
					switch(token.Code()) {
						case TokenCode.dot:
							this.left = new ExpressionNode(token.Position(), this.left, ".", TokenCode.dot);
							this.left.Parse(ref tokenQueue);
							step = 1;
							tokenQueue.Dequeue();
							break;

						case TokenCode.logic_op:
							this.operation = (string)token.Value();
							this.priorityCode = token.Code();
							tokenQueue.Dequeue();
							break;

						case TokenCode.relation_op:
							this.operation = (string)token.Value();
							this.priorityCode = token.Code();
							tokenQueue.Dequeue();
							break;

						case TokenCode.factor_op:
							this.operation = (string)token.Value();
							this.priorityCode = token.Code();
							tokenQueue.Dequeue();
							break;

						case TokenCode.term_op:
							this.operation = (string)token.Value();
							this.priorityCode = token.Code();
							tokenQueue.Dequeue();
							break;

						default:
							step = 4;
							break;
					}
					break;

				case 2:
					step = 3;
					switch(token.Code()) {
						case TokenCode.logic_op when (string)token.Value() == "not":
							tokenQueue.Dequeue();
							this.right = new ExpressionNode(token.Position(), new PrimaryNode(token.Position(), 0), "not", TokenCode.logic_op);
							this.right.Parse(ref tokenQueue);
							break;

						case TokenCode.left_parenthesis:
							this.right = new ExpressionNode(token.Position());
							this.right.Parse(ref tokenQueue);
							break;

						case TokenCode.identifier:
							tokenQueue.Dequeue();
							if(tokenQueue.Peek().Code() == TokenCode.dot)
								this.right = new FieldAccessNode(token.Position(), this.right);
							else {
								this.right = new PrimaryNode(token.Position(), token.Value());
								tokenQueue.Dequeue();
							}
							break;

						case TokenCode.constant_value:
							this.right = new PrimaryNode(token.Position(), token.Value());
							tokenQueue.Dequeue();
							break;

						default:
							step = 4;
							break;
					}
					break;

				case 3:
					step = 2;
					switch(token.Code()) {
						case TokenCode.logic_op:
							this.left = new ExpressionNode(this.position, this.left, this.operation, this.priorityCode, this.right);
							this.priorityCode = token.Code();
							this.operation = (string)token.Value();
							this.position = token.Position();
							tokenQueue.Dequeue();
							break;

						case TokenCode.relation_op:
							if(this.priorityCode == TokenCode.logic_op) {
								tokenQueue.Dequeue();
								this.right = new ExpressionNode(token.Position());
								this.right.Parse(ref tokenQueue);
							} else {
								this.left = new ExpressionNode(this.position, this.left, this.operation, this.priorityCode, this.right);
								this.priorityCode = token.Code();
								this.operation = (string)token.Value();
								this.position = token.Position();
								tokenQueue.Dequeue();
							}
							break;

						case TokenCode.factor_op:
							if(this.priorityCode == TokenCode.term_op) {
								this.left = new ExpressionNode(this.position, this.left, this.operation, this.priorityCode, this.right);
								this.priorityCode = token.Code();
								this.operation = (string)token.Value();
								this.position = token.Position();
								tokenQueue.Dequeue();
							} else {
								tokenQueue.Dequeue();
								this.right = new ExpressionNode(token.Position());
								this.right.Parse(ref tokenQueue);
							}
							break;

						case TokenCode.term_op:
							tokenQueue.Dequeue();
							this.right = new ExpressionNode(token.Position(), this.right, (string)token.Value(), token.Code());
							this.right.Parse(ref tokenQueue);
							tokenQueue.Dequeue();
							break;

						default:
							step = 4;
							break;
					}
					break;

				default:
					break;
			}
		}
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ExpressionNode") Console.WriteLine($"ExpressionNode(childs={this.childs.Count}, operation={this.operation}, initialized={this.initialized}, const={this.cnst}, left?={this.left != null}, right?={this.right != null})");
		base.PrintInfo(indent);
	}
}
}
