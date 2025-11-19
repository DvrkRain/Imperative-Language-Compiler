using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

using CodeGen;
using System;
using System.Reflection;
using System.Reflection.Emit;


namespace AST;
public class WhileNode : Node {
	public WhileNode(Position pos) : base(pos) { }

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "WhileNode") Console.WriteLine($"WhileNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


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


    public override void Verify() 
    {
        SymbolTable.EnterScope(ScopeType.Loop);

		if(Returning.Count() > 0)
			Returning.Push(ReturningStatus.Copy(Returning.Peek()));
        base.Verify();
		if(Returning.Count() > 0)
			Returning.Pop();

        
        // WhileLoop declaration looks like
        // while `Expression` loop `Body` end
        
        // `Expression` -> ExpressionNode
        // `Body` -> ProgramNode
        
        if (this.childs.Count != 2) {
            ErrorHandling.Add("WhileNode", this.position, $"Expected 2 childs, got  {this.childs.Count}");
            SymbolTable.ExitScope();
            return;
        }

        if (!VerifyExpression() || !VerifyBody()) {
            SymbolTable.ExitScope();
            return;
        }

        SymbolTable.ExitScope();
    }

    private bool VerifyExpression() {
        if (this.childs[0] is not ExpressionNode) {
            ErrorHandling.Add($"WhileNode", this.position, "Expected ExpressionNode");
            return false;
        }
        
        ExpressionNode expression = (ExpressionNode)this.childs[0];
        
        // Check expression result type == boolean
        if (expression.Type() != "boolean") {
            ErrorHandling.Add("WhileNode", this.position, $"Expected boolean, got  {expression.Type()}");
            return false;
        }

        return true;
    }

    private bool VerifyBody() {
        if (this.childs[1] is not ProgramNode) {
            ErrorHandling.Add($"WhileNode", this.position, "Expected ProgramNode");
            return false;
        }
        
        return true;
    }
    
    public override void Generate(CodeGenContext ctx)
    {
        Label startLabel = ctx.CurrentIL.DefineLabel();
        Label endLabel = ctx.CurrentIL.DefineLabel();
    
        ctx.EnterLoop(endLabel, startLabel);
    
        ctx.CurrentIL.MarkLabel(startLabel);
    
        // Condition
        this.childs[0].Generate(ctx);
        ctx.CurrentIL.Emit(OpCodes.Brfalse, endLabel);
    
        // Body
        this.childs[1].Generate(ctx);
        ctx.CurrentIL.Emit(OpCodes.Br, startLabel);
    
        ctx.CurrentIL.MarkLabel(endLabel);
    
        ctx.ExitLoop();
    }

}
