using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;

namespace AST;

public class ContinueNode : Node {
    public ContinueNode(Position pos) : base(pos) {}

    public override void PrintInfo(string indent) {
        if (this.GetType() == typeof(ContinueNode)) Console.WriteLine($"ContinueNode(pos=({this.position.Row()}, {this.position.Col()}))");
        base.PrintInfo(indent);
    }

    public override void Parse(ref Queue<Token> tokenQueue) {
        // After continue there should be only ';'
        Token token = tokenQueue.Peek();

        if (token.Code() != TokenCode.semicolon) {
            HandleUnexpectedToken(ref tokenQueue, this.position);
            return;
        }
        
        tokenQueue.Dequeue();
    }

    public override void Verify() {
        if (!SymbolTable.IsInsideType(ScopeType.Loop)) {
            ErrorHandling.Add("ContinueNode", this.position, "'continue' used outside loop");
            return;
        }

        base.Verify();
    }
}