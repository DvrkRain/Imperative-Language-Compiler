using Compiler.Data;
using System.Reflection;
using System.Reflection.Emit;

namespace Compiler.AST;
public class ProgramNode : Node {
	public bool main;
	public bool returned;

    public ProgramNode(Position pos, string returnType = "void", bool main = false) : base(pos) {
        this._type = returnType;
        this.main = main;
    }
        
	public override void PrintInfo(string indent) {
		Console.WriteLine($"ProgramNode(pos={this.position.ToString()}, main={this.main})");
		base.PrintInfo(indent);
	}

	
	public override void Parse(ref Queue<Token> tokenQueue) {
		bool parsing = true;
		while(parsing && tokenQueue.Count > 0) {
			Token token = tokenQueue.Peek();
			switch(token.Code()) {
				// Variable declaration
				case TokenCode.variable_declaration:
					tokenQueue.Dequeue();
					VarNode var_decl = new VarNode(token.Position());
					var_decl.Parse(ref tokenQueue);
					this.childs.Add(var_decl);
					break;
					
				// Type alias declaration
				case TokenCode.type_declaration:
					tokenQueue.Dequeue();
					TypeNode type_decl = new TypeNode(token.Position());
					type_decl.Parse(ref tokenQueue);
					this.childs.Add(type_decl);
					break;
					
				// Routine declaration
				case TokenCode.routine_declaration:
					tokenQueue.Dequeue();
					RoutineNode rout_decl = new RoutineNode(token.Position());
					rout_decl.Parse(ref tokenQueue);
					this.childs.Add(rout_decl);
					break;
					
				// If
				case TokenCode.if_statement:
					tokenQueue.Dequeue();
					IfNode if_stnt = new IfNode(token.Position());
					if_stnt.Parse(ref tokenQueue);
					this.childs.Add(if_stnt);
					break;
					
				// For
				case TokenCode.for_statement:
					tokenQueue.Dequeue();
					ForNode for_stnt = new ForNode(token.Position());
					for_stnt.Parse(ref tokenQueue);
					this.childs.Add(for_stnt);
					break;
					
				// While
				case TokenCode.while_statement:
					tokenQueue.Dequeue();
					WhileNode while_stnt = new WhileNode(token.Position());
					while_stnt.Parse(ref tokenQueue);
					this.childs.Add(while_stnt);
					break;

				// Assignment
				case TokenCode.identifier:
					if(tokenQueue.ElementAt(1).Code() == TokenCode.left_parenthesis) {
						ExpressionNode expr = new ExpressionNode(this.position);
						expr.Parse(ref tokenQueue);
						this.childs.Add(expr);
					} else if(tokenQueue.ElementAt(1).Code() == TokenCode.bare_assignment
							|| tokenQueue.ElementAt(1).Code() == TokenCode.left_bracket
							|| tokenQueue.ElementAt(1).Code() == TokenCode.dot) {
						tokenQueue.Dequeue();
						FieldAccessNode access = new FieldAccessNode(token.Position(), new PrimaryNode(token.Position(), token.Value()));
						access.Parse(ref tokenQueue);
						tokenQueue.Dequeue();
						AssignmentNode asgnmt = new AssignmentNode(token.Position(), access);
						asgnmt.Parse(ref tokenQueue);
						this.childs.Add(asgnmt);
					} else {
						HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "funtcion call or assignment");
						parsing = false;
                        break;
					}
					break;
					
				// Sequence break
				case TokenCode.return_statement:
					tokenQueue.Dequeue();
					ReturnNode ret = new ReturnNode(token.Position());
					ret.Parse(ref tokenQueue);
					this.childs.Add(ret);
					break;

				case TokenCode.break_statement:
					tokenQueue.Dequeue();
					BreakNode brk = new BreakNode(token.Position());
					brk.Parse(ref tokenQueue);
					this.childs.Add(brk);
					break;

				case TokenCode.continue_statement:
					tokenQueue.Dequeue();
					ContinueNode cnt = new ContinueNode(token.Position());
					cnt.Parse(ref tokenQueue);
					this.childs.Add(cnt);
					break;

				// Print
				case TokenCode.print_routine:
					tokenQueue.Dequeue();
					PrintNode prt = new PrintNode(token.Position());
					prt.Parse(ref tokenQueue);
					this.childs.Add(prt);
					break;

				// End of body
				case TokenCode.end_of_body:
					tokenQueue.Dequeue();
					parsing = false;
					break;

				case TokenCode.else_statement:
					parsing = false;
					break;

				case TokenCode.end_of_file:
					tokenQueue.Dequeue();
					if(!this.main) ErrorHandling.UnexpectedEOF("ProgramNode", this.position);
					parsing = false;
					break;

				case TokenCode.semicolon:
					tokenQueue.Dequeue();
					break;

				default:
					HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "command");
					break;
			}
		}
	}


    public override void Verify() {
        base.Verify();
        
        int lastIndex = this.childs.Count() - 1;
        for (int i = 0; i <= lastIndex; i++) {
            switch (this.childs[i]) {
                case ReturnNode:
                    if (SymbolTable.IsInsideType(ScopeType.Routine)) lastIndex = i;
					this.returned = true;
                    break;
                case BreakNode or ContinueNode:
                    if (SymbolTable.IsInsideType(ScopeType.Loop)) lastIndex = i;
                    break;
            }
        }
        
        this.childs.RemoveRange(lastIndex + 1, this.childs.Count() - (lastIndex + 1));
    }

    public override void Generate(CodeGen.CodeGenContext ctx)
    {
        if (this.main)
        {
            // Create Main method
            var mainMethod = ctx.ProgramTypeBuilder.DefineMethod(
                "_Main",
                MethodAttributes.Public | MethodAttributes.Static,
                typeof(void),
                System.Type.EmptyTypes);
        
            ctx.CurrentMethod = mainMethod;
            ctx.CurrentIL = mainMethod.GetILGenerator();
            ctx.LocalVariables.Clear();
        }
    
        // Process children
        foreach (var child in this.childs) child.Generate(ctx);
    
        if (this.main && ctx.CurrentIL != null)
            ctx.CurrentIL.Emit(OpCodes.Ret);
    }

}
