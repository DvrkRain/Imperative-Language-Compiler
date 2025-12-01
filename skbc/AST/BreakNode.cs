using Data.Objects;
using Data.ErrorHandling;
using System.Reflection.Emit;

namespace Compiler.AST;
public class BreakNode : Node {
    public BreakNode(Position pos) : base(pos) {}

    public override void PrintInfo(string indent) {
        Console.WriteLine($"BreakNode(pos={this.position.ToString()})");
        base.PrintInfo(indent);
    }


    public override void Parse(ref Queue<Token> tokenQueue) {
        // After break there should be only ';'
        Token token = tokenQueue.Peek();

        if (token.Code() != TokenCode.semicolon) {
            HandleUnexpectedToken(ref tokenQueue, this.position, token.Code(), "semicolon");
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
    

    public override void Generate(CodeGen.CodeGenContext ctx) =>
        ctx.CurrentIL.Emit(OpCodes.Br, ctx.CurrentLoop.BreakLabel);
}
