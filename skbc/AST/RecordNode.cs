using Data.ErrorHandling;
using Data.Objects;

namespace Compiler.AST;
public class RecordNode : Node {
	public RecordNode(Position pos) : base(pos) { }

	public override void PrintInfo(string indent) {
		Console.WriteLine($"RecordNode(pos={this.position.ToString()})");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		Token token = tokenQueue.Peek();
		while(token.Code() != TokenCode.end_of_body) {
			if(token.Code() != TokenCode.variable_declaration) {
				HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "field variable declaration");
				return;
			}
			tokenQueue.Dequeue();
			VarNode field = new VarNode(token.Position());
			field.Parse(ref tokenQueue);
			this.childs.Add(field);
			token = tokenQueue.Peek();
		}
		tokenQueue.Dequeue();
	}


    public override void Verify() {
        foreach (var child in childs) {
            if (child is not VarNode) {
                ErrorHandling.Add("RecordNode", this.position, $"RecordNode should contain only VarNodes");
                return;
            }
        }
        base.Verify();
    }
}
