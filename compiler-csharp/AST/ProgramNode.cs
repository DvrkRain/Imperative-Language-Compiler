using Data.Objects;
namespace AST {
public class ProgramNode : Node {
	public bool main;

	public ProgramNode(Position pos) : base(pos) { }

	
	public override void Parse(ref Queue<Token> tokenQueue) {
		bool parsing = true;
		while(parsing && tokenQueue.Count > 0) {
			Token token = tokenQueue.Dequeue();
			switch(token.Code()) {
				// Variable declaration
				case TokenCode.variable_declaration:
					VarNode var_decl = new VarNode(token.Position());
					var_decl.Parse(ref tokenQueue);
					this.childs.Add(var_decl);
					break;
					
				// Type alias declaration
				case TokenCode.type_declaration:
					TypeNode type_decl = new TypeNode(token.Position());
					type_decl.Parse(ref tokenQueue);
					this.childs.Add(type_decl);
					break;
					
				// Routine declaration
				case TokenCode.routine_declaration:
					RoutineNode rout_decl = new RoutineNode(token.Position());
					rout_decl.Parse(ref tokenQueue);
					this.childs.Add(rout_decl);
					break;
					
				// If
				case TokenCode.if_statement:
					IfNode if_stnt = new IfNode(token.Position());
					if_stnt.Parse(ref tokenQueue);
					this.childs.Add(if_stnt);
					break;
					
				// For
				case TokenCode.for_statement:
					ForNode for_stnt = new ForNode(token.Position());
					for_stnt.Parse(ref tokenQueue);
					this.childs.Add(for_stnt);
					break;
					
				// While
				case TokenCode.while_statement:
					WhileNode while_stnt = new WhileNode(token.Position());
					while_stnt.Parse(ref tokenQueue);
					this.childs.Add(while_stnt);
					break;

				// Assignment
				// TODO
				case TokenCode.identifier:
					switch(tokenQueue.Peek().Code()) {
						case TokenCode.dot:
							// TODO
							// Node field_acc = new ExpressionNode(new PrimaryNode(token.value));
							// field_acc.Parse(ref tokenQueue);
							// Node asgnmt = new AssignmentNode(field_acc);
							// asgnmt.Parse(ref tokenQueue);
							break;
							
						case TokenCode.bare_assignment:
							Node asgnmt = new AssignmentNode(token.Position(), new PrimaryNode(token.Position(), token.Value()));
							asgnmt.Parse(ref tokenQueue);
							this.childs.Add(asgnmt);
							break;

						default:
							break;
					}
					break;
					
				case TokenCode.end_of_body:
					parsing = false;
					break;

				case TokenCode.semicolon:
					break;

				default:
					HandleUnexpectedToken(ref tokenQueue);
					break;
			}
		}
	}

	public override void PrintInfo(string indent) {
		Console.WriteLine($"ProgramNode(childs={this.childs.Count}, main={this.main})");
		base.PrintInfo(indent);
	}
}
}
