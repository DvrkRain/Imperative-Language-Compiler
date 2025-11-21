using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;

using CodeGen;
using System;
using System.Reflection;
using System.Reflection.Emit;

using SystemType = System.Type;

namespace AST;
public class RoutineNode : Node {
	bool has_body = false;
	bool implementation = false;
	public RoutineNode(Position pos) : base(pos) { }

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "RoutineNode") Console.WriteLine($"RoutineNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()})");
		base.PrintInfo(indent);
	}


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
				this._type = (string)token.Value();
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
				ProgramNode body = new ProgramNode(tokenQueue.Peek().Position());
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


    public override void Verify() {
        // Routine declaration looks like
        // RoutineHeader [RoutineBody]
        
        // RoutineHeader looks like
        // routine `Identifier` (`Parameters`) [:`Type`]
        
        // `Identifier` -> PrimaryNode
        // `Parameters` -> ParameterNode (1-inf amount)
        // `Type` -> PrimaryNode
        
        // RoutineBody -> ProgramNode

		int param_number = 0;
		foreach(var child in childs) {
			if(child is ParameterNode) param_number++;
		}

        // Check child type
        if (this.childs[0] is not PrimaryNode) {
            ErrorHandling.Add("RoutineNode", this.position, $"Expected PrimaryNode as identifier, got {this.childs[0].GetType().Name}");
            return;
        }

		// Check if returning type is declared
        if (this._type != "void" && SymbolTable.FindEntry(this._type) is not SemanticAnalyzer.SymbolTable.Type) {
            ErrorHandling.Add("RoutineNode", this.position, $"Return type not declared, got {this._type}");
            return;
        }

        // Routines are defined only in global scope
        if (!SymbolTable.IsInsideType(ScopeType.Global, true)) {
            ErrorHandling.Add("RoutineNode", this.position, "Routine can be defined only in global scope");
            return;
        }

        List<Variable> parameters = new List<Variable>();
		this.has_body = this.childs.Count() == 2+param_number;
		string identifier = (string)((PrimaryNode)this.childs[0]).value;

        if (!VerifyParameters(param_number)) return;

        SymbolTable.EnterScope(ScopeType.Routine);
        Routine thisRoutine = (Routine)SymbolTable.FindEntry(identifier);

		Returning.Push(new ReturningStatus(false, this._type));
        base.Verify();
		ReturningStatus stat = Returning.Pop();
		if(this._type != "void" && this.has_body && this.childs.Last() is not ExpressionNode && !stat.returned) {
			ErrorHandling.Add("RoutineNode", this.position, "Routine has returning type, but return is not guaranteed");
			SymbolTable.ExitScope();
			return;
		}

        Scope curScope = SymbolTable.GetCurrentScope();
        SymbolTable.ExitScope();
        curScope.Parent = null;

		// Check redeclaration
		switch(SymbolTable.FindEntry(identifier)) {
			case Routine routine when routine is {HasBody: true}:
				ErrorHandling.Add("RoutineNode", this.position, "Routine redeclaration");
				return;

			case Routine routine:
				if(!this.has_body) {
					ErrorHandling.Add("RoutineNode", this.position, "Forward routine redeclaration");
					return;
				} else if(routine.ReturnType != this._type) {
					ErrorHandling.Add("RoutineNode", this.position,
							$"Redeclaring routine with unmatched type: expected {this._type}, got {routine.ReturnType}.");
					return;
				}
                routine.HasBody = true;
                parameters = routine.Parameters;
				this.implementation = true;
				break;

			case null:
                for(int i=1; i<1+param_number; i++) {
                    ParameterNode param = (ParameterNode)this.childs[i];
                    parameters.Add(new Variable(param.Name(), param.Type()));
                }

                Routine newRoutine = new Routine(identifier, parameters, this._type);
                newRoutine.HasBody = this.has_body;

                SymbolTable.DeclareEntry(newRoutine);
				break;

			default:
				ErrorHandling.Add("RoutineNode", this.position, $"Identifier '{identifier}' already exists and is not a routine");
				return;
		}
        
        if(thisRoutine != null) thisRoutine.BodyScope = curScope;
    }

    private bool VerifyParameters(int param_num) {
        PrimaryNode identifier = (PrimaryNode)this.childs[0];
        List<Variable> prevRoutineParams = null;
        HashSet<string> paramNames = new HashSet<string>();

        if (SymbolTable.FindEntry((string)identifier.value) is Routine prevRoutine) {
            prevRoutineParams = prevRoutine.Parameters;
            if (param_num != prevRoutineParams.Count) {
                ErrorHandling.Add("RoutineNode", this.position, "Parameter amount mismatch");
                return false;
            }
        }
        
        for(int i=1; i<1+param_num; i++) {
            ParameterNode param = (ParameterNode)this.childs[i];
            string paramName = param.Name();

            if (!paramNames.Add(paramName)) {
                ErrorHandling.Add("RoutineNode", this.position, $"Parameter names must be unique, got {paramName}");
                return false;
            }

            if (prevRoutineParams is null) continue;

            if (prevRoutineParams[i - 1].Name != paramName) {
                ErrorHandling.Add("RoutineNode", this.position,
						$"Parameter name mismatch, expected {prevRoutineParams[i - 1].Name}, got {paramName}.");
                return false;
            }
            
            string paramType = param.Type();

            if (prevRoutineParams[i - 1].Type != paramType) {
                ErrorHandling.Add("RoutineNode", this.position,
						$"Parameter type mismatch, expected {prevRoutineParams[i - 1].Type}, got {paramType}.");
                return false;
            }
        }

        return true;
    }
    
    public override void Generate(CodeGenContext ctx) {
		string routineName = (string)((PrimaryNode)this.childs[0]).value;

		var paramTypes = new List<SystemType>();
		var paramNames = new List<string>();
		int idx = 1;
		
		if(!this.has_body) {
			// Collect parameters
			while (idx < this.childs.Count && this.childs[idx] is ParameterNode)
			{
				var param = (ParameterNode)this.childs[idx];
				string pName = ((PrimaryNode)param.GetChilds()[0]).Name();
				string pType = (string)((PrimaryNode)param.GetChilds()[1]).value;
				paramNames.Add(pName);
				paramTypes.Add(ctx.ResolveType(pType));
				idx++;
			}
		
			SystemType returnType = ctx.ResolveType(this._type);
		
			// Create method
			var method = ctx.ProgramTypeBuilder.DefineMethod(
				routineName,
				MethodAttributes.Public | MethodAttributes.Static,
				returnType,
				paramTypes.ToArray());
		
			ctx.Methods[routineName] = method;
		} else {
			if(!implementation) {
				// Collect parameters
				while (idx < this.childs.Count && this.childs[idx] is ParameterNode)
				{
					var param = (ParameterNode)this.childs[idx];
					string pName = ((PrimaryNode)param.GetChilds()[0]).Name();
					string pType = (string)((PrimaryNode)param.GetChilds()[1]).value;
					paramNames.Add(pName);
					paramTypes.Add(ctx.ResolveType(pType));
					idx++;
				}
			
				SystemType returnType = ctx.ResolveType(this._type);
			
				// Create method
				var method = ctx.ProgramTypeBuilder.DefineMethod(
					routineName,
					MethodAttributes.Public | MethodAttributes.Static,
					returnType,
					paramTypes.ToArray());

				ctx.Methods[routineName] = method;
			} else {
				var method = ctx.Methods[routineName];
				// Save context
				var prevMethod = ctx.CurrentMethod;
				var prevIL = ctx.CurrentIL;
				var prevLocals = new Dictionary<string, System.Reflection.Emit.LocalBuilder>(ctx.LocalVariables);
				var prevParamIndices = new Dictionary<string, int>(ctx.ParameterIndices);
				var prevParamTypes = new Dictionary<string, SystemType>(ctx.ParameterTypes);
			
				SystemType returnType = ctx.ResolveType(this._type);
			
				// New context for routine body
				ctx.CurrentMethod = method;
				ctx.CurrentIL = method.GetILGenerator();
				ctx.LocalVariables.Clear();
				ctx.ClearParameters();
			
				// Map parameters to arguments
				for (int i = 0; i < paramNames.Count; i++)
				{
					ctx.ParameterIndices[paramNames[i]] = i;
					ctx.ParameterTypes[paramNames[i]] = paramTypes[i];
				}
			
				// Generate body
				Node body = this.childs[idx]; // Last child is body
				body.Generate(ctx);
			
				// Ensure return
				if(this.childs.Last() is ExpressionNode)
					ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ret);
				else if (returnType == typeof(void))
					ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ret);
			
				// Restore context
				ctx.CurrentMethod = prevMethod;
				ctx.CurrentIL = prevIL;
				ctx.LocalVariables.Clear();
				foreach (var kvp in prevLocals)
					ctx.LocalVariables[kvp.Key] = kvp.Value;
				
				ctx.ClearParameters();
				foreach (var kvp in prevParamIndices)
					ctx.ParameterIndices[kvp.Key] = kvp.Value;
				foreach (var kvp in prevParamTypes)
					ctx.ParameterTypes[kvp.Key] = kvp.Value;
			}
		}
    }

}
