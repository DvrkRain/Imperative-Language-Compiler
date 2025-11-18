using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;

namespace AST;

public class BreakNode : Node {
    public BreakNode(Position pos) : base(pos) {}

    public override void PrintInfo(string indent) {
        if (this.GetType().Name == "BreakNode") Console.WriteLine($"BreakNode(pos=({this.position.Row()}, {this.position.Col()}))");
        base.PrintInfo(indent);
    }

    public override void Parse(ref Queue<Token> tokenQueue) {
        // After break there should be only ';'
        Token token = tokenQueue.Peek();

        if (token.Code() != TokenCode.semicolon) {
            HandleUnexpectedToken(ref tokenQueue, this.position);
            return;
        }
        
        tokenQueue.Dequeue();
    }

    public override void Verify() {
        if (!SymbolTable.IsInsideType(ScopeType.Loop)) {
            ErrorHandling.Add("BreakNode", this.position, "'break' used outside loop");
            return;
        }

        base.Verify();
    }
}