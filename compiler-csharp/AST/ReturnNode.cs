using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

using CodeGen;
using System;
using System.Reflection;
using System.Reflection.Emit;


namespace AST;
public class ReturnNode : Node {
    public ReturnNode(Position pos) : base(pos) { }

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ReturnNode") Console.WriteLine($"ReturnNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		Token token = tokenQueue.Peek();

        if (token.Code() != TokenCode.semicolon) {
            ExpressionNode expression = new ExpressionNode(token.Position());
            expression.Parse(ref tokenQueue);
            this.childs.Add(expression);
        }

        token = tokenQueue.Peek();

        if (token.Code() != TokenCode.semicolon) {
            HandleUnexpectedToken(ref tokenQueue, token.Position());
            return;
        }
        
        tokenQueue.Dequeue();
    }


    public override void Verify() {
        if (!SymbolTable.IsInsideType(ScopeType.Routine)) {
            ErrorHandling.Add("ReturnNode", this.position, "'return' is used outside the routine");
            return;
        }

		if(Returning.Count() == 0) return;
		if(this.childs.Count() == 0) {
            ErrorHandling.Add("ReturnNode", this.position, $"Routine should return the expression of type {this._type}.");
			return;
		}
        base.Verify();

		ReturningStatus stat = Returning.Pop();
		this._type = stat.ret_type;

        if (this._type != "void" && this._type != this.childs[0].Type()) {
			Returning.Push(stat);
            ErrorHandling.Add("ReturnNode", this.position, $"Return type mismatch: expected {this._type}, got {this.childs[0].Type()}.");
            return;
        }

		stat.returned = true;
		Returning.Push(stat);
    }
    
    public override void Generate(CodeGenContext ctx)
    {
        if (this.childs.Count > 0)
        {
            this.childs[0].Generate(ctx); // Push return value
        }
        ctx.CurrentIL.Emit(OpCodes.Ret);
    }

}
