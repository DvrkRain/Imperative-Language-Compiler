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

        List<List<int>> childsIndexes = new List<List<int>>();

        for (int i = 0; i < 4; i++) {
            childsIndexes.Add(new List<int>());
        }

        if (this.childs.Count() < 2) {
            ErrorHandling.Add("RoutineNode", this.position, "Expected at least 2 childs");
            return;
        }

        if (this.childs[0] is not PrimaryNode) {
            ErrorHandling.Add("RoutineNode", this.position, "Expected identifier as PrimaryNode");
            return;
        }
        childsIndexes[0].Add(0);
        
        if (this.childs[1] is not ParameterNode) {
            ErrorHandling.Add("RoutineNode", this.position, "Expected parameter");
            return;
        }
        
        childsIndexes[1].Add(1);

        for (int i = 2; i < this.childs.Count(); i++) {
            switch (this.childs[i]) {
                case ParameterNode:
                    childsIndexes[1].Add(i);
                    break;
                case PrimaryNode:
                    childsIndexes[2].Add(i);
                    break;
                case ProgramNode or ExpressionNode:
                    childsIndexes[3].Add(i);
                    break;
                default:
                    ErrorHandling.Add("ProgramNode", this.position, "Wrong node type");
                    return;
            }
        }
        
        if (!VerifyIdentifier() || !VerifyParameters(childsIndexes[1]) || 
            !VerifyReturnType(childsIndexes[2]) || !VerifyBody(childsIndexes[3])) return;
        
        string identifier = (string)((PrimaryNode)this.childs[0]).value;
        List<Variable> parameters = new List<Variable>();

        switch (SymbolTable.FindEntry(identifier)) {
            case Routine routine: // Found routine -> redeclaration
                routine.HasBody = true;
                parameters = routine.Parameters;
                break;
            case null: // New routine
                foreach (int paramIndex in childsIndexes[1]) {
                    ParameterNode param = (ParameterNode)this.childs[paramIndex];
                    string paramName = (string)((PrimaryNode)param.GetChilds()[0]).value;
                    string paramType = (string)((PrimaryNode)param.GetChilds()[1]).value;
                    parameters.Add(new Variable(paramName, paramType));
                }

                string returnType = childsIndexes[2].Any()
                    ? (string)((PrimaryNode)this.childs[childsIndexes[2][0]]).value
                    : "void";
                bool hasBody = childsIndexes[3].Any();

                Routine newRoutine = new Routine(identifier, parameters, returnType);
                newRoutine.HasBody = hasBody;

                SymbolTable.DeclareEntry(newRoutine);
                break;
            default:
                ErrorHandling.Add("RoutineNode", this.position, $"Identifier '{identifier}' already exists");
                return;
        }
        
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
        
        Entry? entry = SymbolTable.FindEntry((string)identifier.value);
        if (entry is null) return true;
        if (entry is not Routine) {
            ErrorHandling.Add("RoutineNode", this.position, $"Identifier '{identifier.value}' already exists");
            return false;
        } 

        Routine routine = (Routine) entry;
        
        // If there's routine with body, it's redeclaration
        if (routine is { HasBody: true }) {
            ErrorHandling.Add("RoutineNode", this.position, "Routine redeclaration");
            return false;
        }

        return true;
    }

    private bool VerifyParameters(List<int> paramIndexes) {
        PrimaryNode identifier = (PrimaryNode)this.childs[0];
        List<Variable> prevRoutineParams = null;
        HashSet<string> paramNames = new HashSet<string>();

        if (SymbolTable.FindEntry((string)identifier.value) is Routine prevRoutine) {
            prevRoutineParams = prevRoutine.Parameters;
            if (paramIndexes.Count != prevRoutineParams.Count) {
                ErrorHandling.Add("RoutineNode", this.position, "Parameter amount mismatch");
                return false;
            }
        }
        
        foreach (var i in paramIndexes) {
            ParameterNode param = (ParameterNode)this.childs[i];
            string paramName = (string)((PrimaryNode)param.GetChilds()[0]).value;

            if (!paramNames.Add(paramName)) {
                ErrorHandling.Add("RoutineNode", this.position, $"Parameter names must be unique, got {paramName}");
                return false;
            }

            if (prevRoutineParams is null) continue;

            if (prevRoutineParams[i - 1].Name != paramName) {
                ErrorHandling.Add("RoutineNode", this.position, "Parameter name mismatch");
                return false;
            }
            
            string paramType = (string)((PrimaryNode)param.GetChilds()[1]).value;

            if (prevRoutineParams[i - 1].Type != paramType) {
                ErrorHandling.Add("RoutineNode", this.position, "Parameter type mismatch");
                return false;
            }
        }

        return true;
    }

    private bool VerifyReturnType(List<int> returnTypeIndex) {
        string identifier = (string)((PrimaryNode)this.childs[0]).value;

        if (SymbolTable.FindEntry(identifier) is Routine routine) {
            if (returnTypeIndex.Any() != (routine.ReturnType == "void")) {
                ErrorHandling.Add("RoutineNode", this.position, "Return type mismatch");
                return false;
            }
        }
        
        if (!returnTypeIndex.Any()) return true;
        
        PrimaryNode returnType = (PrimaryNode)this.childs[returnTypeIndex[0]];
        if (SymbolTable.FindEntry((string)returnType.value) is not SemanticAnalyzer.SymbolTable.Type) {
            ErrorHandling.Add("RoutineNode", this.position, $"Return type not declared, got {returnType.value}");
            return false;
        }

        return true;
    }

    private bool VerifyBody(List<int> bodyIndex) {
        string identifier = (string)((PrimaryNode)this.childs[0]).value;

        Entry? entry = SymbolTable.FindEntry(identifier);
        if (entry is null) return true;
        if (entry is not Routine) return false;
        
        Routine routine = (Routine)entry;

        if (bodyIndex.Any() == routine.HasBody) {
            ErrorHandling.Add("RoutineNode", this.position, "Routine redeclaration");
            return false;
        }

        return true;
    }
}
