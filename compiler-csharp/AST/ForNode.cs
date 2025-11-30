using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;
using System.Reflection.Emit;


namespace AST;
public class ForNode : Node {
	protected bool reversed;
	public ForNode(Position pos) : base(pos) => this.reversed = false;

	public override void PrintInfo(string indent) {
		Console.WriteLine($"ForNode(childs={this.childs.Count}, pos={this.position.ToString()})");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Iterator identifier
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "iterator identifier");
			return;
		}
		tokenQueue.Dequeue();
		PrimaryNode id = new PrimaryNode(token.Position(), token.Value());
        this.childs.Add(id);

		// 'in' keyword
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.in_statement) {
			HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "'in' keyword");
			return;
		}
		tokenQueue.Dequeue();

		// First expression
		ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);
		
		// Second expression (optional)
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.range_sign) {
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
			expr = new ExpressionNode(token.Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
			token = tokenQueue.Peek();
		}

		// Reverse (optional)
		if(token.Code() == TokenCode.reverse_statement) {
			this.reversed = true;
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
		}
		// 'loop' keyword
		if(token.Code() != TokenCode.loop_start) {
			HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "'loop' keyword");
			return;
		}
		tokenQueue.Dequeue();

		// Loop body
		ProgramNode nested = new ProgramNode(token.Position());
		nested.Parse(ref tokenQueue);
		this.childs.Add(nested);
	}


    public override void Verify() {
        SymbolTable.EnterScope(ScopeType.Loop);

		if(Returning.Count() > 0)
			Returning.Push(ReturningStatus.Copy(Returning.Peek()));
		SymbolTable.DeclareEntry(new Variable((string)(((PrimaryNode)this.childs[0]).value), "integer"));
        base.Verify();
		if(Returning.Count() > 0)
			Returning.Pop();

        SymbolTable.ExitScope();

        ExpressionNode firstExpression = (ExpressionNode)this.childs[1];

        if (firstExpression.Type() != "integer") {
            ErrorHandling.Add("ForNode", this.position, "ForNode should iterate on integer values");
            return;
        }

        if (this.childs[2] is ExpressionNode secondExpression && secondExpression.Type() != "integer") {
            ErrorHandling.Add("ForNode", this.position, "ForNode should iterate on integer values");
            return;
        }

		if(this.childs.Count() == 4 && this.reversed)
			(this.childs[1], this.childs[2]) = (this.childs[2], this.childs[1]);
    }

    
    public override void Generate(CodeGen.CodeGenContext ctx) {
        string iterName = (string)((PrimaryNode)this.childs[0]).value;
        var iterLocal = ctx.CurrentIL.DeclareLocal(typeof(int));
        ctx.LocalVariables[iterName] = iterLocal;
    
        // Initialize: iter = start
        this.childs[1].Generate(ctx);
        ctx.CurrentIL.Emit(OpCodes.Stloc, iterLocal);
    
		// Labels for workflow control
        Label startLabel = ctx.CurrentIL.DefineLabel();
        Label endLabel = ctx.CurrentIL.DefineLabel();
        Label continueLabel = ctx.CurrentIL.DefineLabel();
    
        ctx.EnterLoop(endLabel, continueLabel);
    
        ctx.CurrentIL.MarkLabel(startLabel);
    
        // Condition: iter <= end (or >= for reverse)
        ctx.CurrentIL.Emit(OpCodes.Ldloc, iterLocal);
        this.childs[2].Generate(ctx);
        ctx.CurrentIL.Emit(this.reversed ? OpCodes.Clt : OpCodes.Cgt);
        ctx.CurrentIL.Emit(OpCodes.Brtrue, endLabel);
    
        // Body
        this.childs[3].Generate(ctx);
    
        // Increment/Decrement
        ctx.CurrentIL.MarkLabel(continueLabel);
        ctx.CurrentIL.Emit(OpCodes.Ldloc, iterLocal);
        ctx.CurrentIL.Emit(OpCodes.Ldc_I4_1);
        ctx.CurrentIL.Emit(this.reversed ? OpCodes.Sub : OpCodes.Add);
        ctx.CurrentIL.Emit(OpCodes.Stloc, iterLocal);
        ctx.CurrentIL.Emit(OpCodes.Br, startLabel);
    
        ctx.CurrentIL.MarkLabel(endLabel);
        ctx.ExitLoop();
    }

}
