using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class RoutineNode : Node {
	public RoutineNode(Position pos) : base(pos) { }

	public override void Parse(ref Queue<Token> tokenQueue) {
		// Routine identifier
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();
		this.childs.Add(new PrimaryNode(token.Position(), token.Value()));

		// Left Parenthesis
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.left_parenthesis) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Parsing parameters
		while(true) {
			ParameterNode param = new ParameterNode(tokenQueue.Peek().Position());
			param.Parse(ref tokenQueue);
            this.childs.Add(param);

			token = tokenQueue.Peek();
			if(token.Code() == TokenCode.right_parenthesis) {
				tokenQueue.Dequeue();
				break;
			} else if(token.Code() == TokenCode.comma) {
				tokenQueue.Dequeue();
				continue;
			} else {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
		}

		// Explicit type assignment (optional)
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.type_assignment) {
			tokenQueue.Dequeue();
			token = tokenQueue.Dequeue();
			if(token.Code() == TokenCode.identifier || token.Code() == TokenCode.builtin_type)
				this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
			else
				ErrorHandling.UnexpectedTokenException(this.GetType().Name, token.Position());
			token = tokenQueue.Peek();
		}

		// Body, one-line expression or semicolon for forward declaration
		switch(token.Code()) {
			case TokenCode.semicolon:
				tokenQueue.Dequeue();
				return;

			case TokenCode.is_assignment:
				tokenQueue.Dequeue();
                string? returnType = this.childs[this.childs.Count() - 1] is PrimaryNode ? (string)((PrimaryNode)this.childs[this.childs.Count() - 1]).value : null;
				ProgramNode body = new ProgramNode(tokenQueue.Peek().Position(), returnType);
				body.Parse(ref tokenQueue);
				this.childs.Add(body);
				break;

			case TokenCode.one_line_body:
				tokenQueue.Dequeue();
				ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position());
				expr.Parse(ref tokenQueue);
				this.childs.Add(expr);
				break;

			default:
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
		}
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "RoutineNode") Console.WriteLine($"RoutineNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()})");
		base.PrintInfo(indent);
	}

    public override void Verify() {
        // Routine declaration looks like
        // RoutineHeader [RoutineBody]
        
        // RoutineHeader looks like
        // routine `Identifier` (`Parameters`) [:`Type`]
        
        // `Identifier` -> PrimaryNode
        // `Parameters` -> ParameterNode (1-inf amount)
        // `Type` -> PrimaryNode
        
        // RoutineBody -> ProgramNode

        if (!VerifyIdentifier() || !VerifyParameters() || !VerifyReturnType()) return;
        string routineName = (string)((PrimaryNode)this.childs[0]).value;
        string? returnType = null;
        List<Variable> parameters = new List<Variable>();
        int paramStopIndex = -1;
        
        for (int i = 1; i < this.childs.Count(); i++) {
            if (this.childs[i] is not ParameterNode) {
                paramStopIndex = i;
                break;
            }
            
            ParameterNode param = (ParameterNode)this.childs[i];
            string parameterName = (string)((PrimaryNode)param.GetChilds()[0]).value;
            string parameterType = (string)((PrimaryNode)param.GetChilds()[1]).value;
            parameters.Add(new Variable(parameterName, parameterType));
        }

        if (paramStopIndex != -1) {
            if (this.childs[paramStopIndex] is PrimaryNode) {
                returnType = (string)((PrimaryNode)this.childs[paramStopIndex]).value;
            }
        }

        Routine routineEntry = new Routine(routineName, parameters, returnType);

        if (this.childs[this.childs.Count() - 1] is ProgramNode) routineEntry.HasBody = true;
        
        SymbolTable.DeclareEntry(routineEntry);
        SymbolTable.EnterScope(ScopeType.Routine);
        base.Verify();
        SymbolTable.ExitScope();
    }

    private bool VerifyIdentifier() {
        // Check child type
        if (this.childs[0] is not PrimaryNode) {
            ErrorHandling.Add("RoutineNode", this.position, $"Expected PrimaryNode as identifier, got {this.childs[0].GetType().Name}");
            return false;
        }
        
        PrimaryNode identifier = (PrimaryNode)this.childs[0];
        
        // Routines are defined only in global scope
        if (!SymbolTable.IsInsideType(ScopeType.Global, true)) {
            ErrorHandling.Add("RoutineNode", this.position, "Routine can be defined only in global scope");
            return false;
        }
        
        // If there's routine with body, it's redeclaration
        Routine? routine = (Routine?)SymbolTable.FindEntry((string)identifier.value);
        if (routine is { HasBody: true }) {
            ErrorHandling.Add("RoutineNode", this.position, "Routine redeclaration");
            return false;
        }

        return true;
    }

    private bool VerifyParameters() {
        for (int i = 1; i < this.childs.Count; i++) {
            if (this.childs[i] is not ParameterNode) break;
            ParameterNode param = (ParameterNode)this.childs[i];
            
            // Risky check: ParameterNode has 2 childs - PrimaryNode(VarName), PrimaryNode(TypeName)
            // Need to check if TypeName is actual type
            PrimaryNode paramType = (PrimaryNode)param.GetChilds()[1];

            if (SymbolTable.FindEntry((string)paramType.value) is not SemanticAnalyzer.SymbolTable.Type) {
                ErrorHandling.Add("RoutineNode", this.position, $"Parameter №{i} type not declared, got {paramType.value}");
                return false;
            }
        }

        return true;
    }

    private bool VerifyReturnType() {
        int found = -1;

        for (int i = 1; i < this.childs.Count(); i++) {
            if (this.childs[i] is PrimaryNode) {
                found = i;
                break;
            }
        }

        if (found == -1) return true;
        
        PrimaryNode returnType = (PrimaryNode)this.childs[found];
        if (SymbolTable.FindEntry((string)returnType.value) is not SemanticAnalyzer.SymbolTable.Type) {
            ErrorHandling.Add("RoutineNode", this.position, $"Return type not declared, got {returnType.value}");
            return false;
        }

        return true;
    }
}
