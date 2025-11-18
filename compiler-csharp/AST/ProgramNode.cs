using Data.ErrorHandling;
using Data.Objects;
namespace AST;
public class ProgramNode : Node {
	public bool main;
    protected bool returned = false;

	public bool Returned() => returned;

    public ProgramNode(Position pos, string returnType = "void") : base(pos) =>
        this._type = returnType;

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ProgramNode") Console.WriteLine($"ProgramNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}), main={this.main})");
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
					tokenQueue.Dequeue();
					FieldAccessNode access = new FieldAccessNode(token.Position());
					access.Parse(ref tokenQueue);
					token = tokenQueue.Peek();
					if(token.Code() != TokenCode.bare_assignment) {
						HandleUnexpectedToken(ref tokenQueue, token.Position());
						parsing = false;
                        break;
                    }
					tokenQueue.Dequeue();
					AssignmentNode asgnmt = new AssignmentNode(token.Position(), access);
					asgnmt.Parse(ref tokenQueue);
					this.childs.Add(asgnmt);
					break;
					
				// Return
				case TokenCode.return_statement:
					tokenQueue.Dequeue();
					ReturnNode ret = new ReturnNode(token.Position(), this._type);
					ret.Parse(ref tokenQueue);
					this.childs.Add(ret);
                    this.returned = true;
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
					parsing = false;
					break;

				case TokenCode.semicolon:
					tokenQueue.Dequeue();
					break;

				default:
					HandleUnexpectedToken(ref tokenQueue, token.Position());
					break;
			}
		}
	}


    public override void Verify() {
		if(this._type != "void")
			Returning.Push(this.returned);

        base.Verify();
		
		if(this._type != "void")
			this.returned = Returning.Pop();

        if (this._type != "void" && !returned) {
            ErrorHandling.Add("ProgramNode", this.position, $"No return statement (or returning is not guaranteed) in function returning {this._type}.");
            return;
        }
    }
}
