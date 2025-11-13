using System.Security.Cryptography;
using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class WhileNode : Node {
	public WhileNode(Position pos) : base(pos) { }


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Condition expression
		ExpressionNode expr = new ExpressionNode(this.position);
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		// 'loop' keyword
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.loop_start) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Loop body
		ProgramNode nested = new ProgramNode(tokenQueue.Peek().Position());
		nested.Parse(ref tokenQueue);
		this.childs.Add(nested);
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "WhileNode") Console.WriteLine($"WhileNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}

    public override void Verify(ref SymbolTable symTab) 
    {
        base.Verify(ref symTab);
        if (this.childs.Count == 0) {
            ErrorHandling.Add("WhileNode", this.position, "No children");
            return;
        }

        if (this.childs[0] is not ExpressionNode) {
            ErrorHandling.Add($"WhileNode", this.position, "Expected ExpressionNode");
        }
        
        ExpressionNode expr = (ExpressionNode)this.childs[0];
        
        // TODO: check for expression type
    }
}
