using Data.ErrorHandling;
using Data.Objects;
using System.Reflection;
using System.Reflection.Emit;

namespace Compiler.AST;
public class PrintNode : Node {
	public PrintNode(Position pos) : base(pos) { }
	
	public override void PrintInfo(string indent) {
		Console.WriteLine($"PrintNode(childs={this.childs.Count}, pos={this.position.ToString()})");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		Token token = tokenQueue.Peek();
		while(token.Code() == TokenCode.comma) {
			tokenQueue.Dequeue();
			expr = new ExpressionNode(tokenQueue.Peek().Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Peek();
		}

		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "semicolon");
			return;
		}
	}


    public override void Verify() {
        base.Verify();

        foreach (var child in childs) {
            if (child is not ExpressionNode) {
                ErrorHandling.Add("PrintNode", this.position, $"PrintNode should contain only ExpressionNodes");
                return;
            }

            if (!DedicatedWords.BuiltIn(child.Type())) {
                ErrorHandling.Add("PrintNode", this.position, $"PrintNode should receive only builtin_type expressions");
                return;
            }
        }
    }
    
    public override void Generate(CodeGen.CodeGenContext ctx) {
        foreach (var child in this.childs) {
            child.Generate(ctx); // Push value onto stack
        
            // Determine type and call appropriate WriteLine
            System.Type exprType = ctx.ResolveType(child.Type());
            var writeLineMethod = typeof(Console).GetMethod(
                "WriteLine",
                new[] { exprType });
        
            ctx.CurrentIL.Emit(OpCodes.Call, writeLineMethod);
        }
    }

}
