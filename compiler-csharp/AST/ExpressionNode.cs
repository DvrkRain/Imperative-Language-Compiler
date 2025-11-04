using Data.Objects;
namespace AST {
public class ExpressionNode : Node {
	private string opCode;
	private Node left;
	private Node right;
	private bool initialized;
	private bool cnst;

	public ExpressionNode(Position pos) : base(pos) {
		this.initialized = false;
		this.cnst = false;
	}
	public ExpressionNode(Position pos, Node init, string operation) : base(pos) {
		this.initialized = true;
		this.left = init;
		this.opCode = operation;
	}
	public ExpressionNode(Position pos, Node init, string operation, Node rightInit)
		: this(pos, init, operation) => this.right = rightInit;


	public override void Parse(ref Queue<Token> tokenQueue) {
		int step = 0;
		if(initialized) step = 2;

		bool parenthesised = false;
		while(step < 3) {
			Token token = tokenQueue.Peek();
			switch(step) {
				case 0:
					switch(token.Code()) {
						case TokenCode.left_parenthesis:
							if(parenthesised) {
								this.left = new ExpressionNode(token.Position());
								this.left.Parse(ref tokenQueue);
								step = 1;
							} else {
								parenthesised = true;
								tokenQueue.Dequeue();
							}
							break;

						case TokenCode.identifier:
							this.left = new PrimaryNode(token.Position(), token.Value());
							step = 1;
							tokenQueue.Dequeue();
							break;

						case TokenCode.constant_value:
							this.left = new PrimaryNode(token.Position(), token.Value());
							step = 1;
							tokenQueue.Dequeue();
							break;

						case TokenCode.logic_op when (string)token.Value() == "not":
							this.left = new PrimaryNode(token.Position(), 0);
							this.opCode = (string)token.Value();
							step = 2;
							tokenQueue.Dequeue();
							break;

						case TokenCode.term_op:
							this.left = new PrimaryNode(token.Position(), 0);
							this.opCode = (string)token.Value();
							step = 2;
							tokenQueue.Dequeue();
							break;

						default:
							HandleUnexpectedToken(ref tokenQueue);
							break;
					}
					break;

				case 1:
					step = 2;
					switch(token.Code()) {
						case TokenCode.dot:
							this.left = new ExpressionNode(token.Position(), this.left, ".");
							this.left.Parse(ref tokenQueue);
							step = 1;
							tokenQueue.Dequeue();
							break;

						case TokenCode.logic_op:
							this.opCode = (string)token.Value();
							tokenQueue.Dequeue();
							break;

						case TokenCode.relation_op:
							this.opCode = (string)token.Value();
							tokenQueue.Dequeue();
							break;

						case TokenCode.factor_op:
							this.opCode = (string)token.Value();
							tokenQueue.Dequeue();
							break;

						case TokenCode.term_op:
							this.opCode = (string)token.Value();
							tokenQueue.Dequeue();
							break;

						default:
							step = 4;
							break;
					}
					break;

				case 2:
					step = 1;
					switch(token.Code()) {
						case TokenCode.logic_op when (string)token.Value() == "not":
							tokenQueue.Dequeue();
							this.right = new ExpressionNode(token.Position(), new PrimaryNode(token.Position(), 0), "not");
							this.right.Parse(ref tokenQueue);
							break;

						case TokenCode.left_parenthesis:
							this.left = new ExpressionNode(token.Position());
							this.left.Parse(ref tokenQueue);
							break;

						case TokenCode.identifier:
							this.left = new PrimaryNode(token.Position(), token.Value());
							tokenQueue.Dequeue();
							break;

						case TokenCode.constant_value:
							this.left = new PrimaryNode(token.Position(), token.Value());
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
		if (this.GetType().Name == "ExpressionNode") Console.WriteLine($"ExpressionNode(childs={this.childs.Count}, opCode={this.opCode}, initialized={this.initialized}, const={this.cnst}, left?={this.left != null}, right?={this.right != null})");
		base.PrintInfo(indent);
	}
}
}
