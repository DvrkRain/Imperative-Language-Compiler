using Data.Objects;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class PrimaryNode : Node {
	private bool _inExpression;
	public object value;

	public string Name() {
		if(this.value is string name) return name;
		return "unknown";
	}

	public PrimaryNode(Position pos, object val, bool inExpr = false) : base(pos) {
		this.value = val;
		this._inExpression = inExpr;
	}

	public override void PrintInfo(string indent) {
		Console.WriteLine($"PrimaryNode(childs={this.childs.Count}, pos={this.position.ToString()}, value={this.value})");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) { }


	public override void Verify() {
		if(!this._inExpression) return;
		switch(this.value) {
			case ExpressionNode expr:
				if(expr.Value() is PrimaryNode prime) {
					this.value = prime.value;
					if(SymbolTable.IsInsideType(ScopeType.Loop)) return;
					this._type = prime.Type();
				}
				break;

			case string id:
				// try {
				// 	int val = int.Parse(id);
				// 	this._type = "integer";
				// } catch (FormatException) {
					switch(SymbolTable.FindEntry(id)) {
						case Variable vr:
							this._type = vr.Type;
							if(SymbolTable.IsInsideType(ScopeType.Loop)) return;
							if(vr.Value is not null
								&& vr.Value is not Scope)
								this.value = vr.Value;
							break;

						default:
							break;
					}
				// } catch (OverflowException) {
				// 	ErrorHandling.Add("PrimaryNode", this.position, "Int overflow");
				// }
				break;

			case Variable var:
				this._type = var.Type;
				if(SymbolTable.IsInsideType(ScopeType.Loop)) return;
				if(var.Value is not null)
					this.value = var.Value;
				else this.value = "unknown";
				break;

			case int val:
				this._type = "integer";
				break;

			case float val:
				this._type = "real";
				break;

			case bool val:
				this._type = "boolean";
				break;

			default: break;
		}
	}

    
    public override void Generate(CodeGen.CodeGenContext ctx) {
		if(!this._inExpression) return;
        switch (this.value) {
            case int intVal:
                CodeGen.ILHelper.EmitLoadInt(ctx.CurrentIL, intVal);
                break;
                
            case float fltVal:
                CodeGen.ILHelper.EmitLoadReal(ctx.CurrentIL, fltVal);
                break;
                
            case bool boolVal:
                CodeGen.ILHelper.EmitLoadBool(ctx.CurrentIL, boolVal);
                break;
                
            case string varName:
                // Check in order: locals -> parameters -> globals
	            if (ctx.LocalVariables.ContainsKey(varName)) {
                    // Load local variable
                    ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldloc, ctx.LocalVariables[varName]);
                } else if (ctx.ParameterIndices.ContainsKey(varName)) {
		            // Load parameter (argument)
		            // TODO: Fix argument load
		            int argIndex = ctx.ParameterIndices[varName];
		            switch (argIndex) {
			            case 0: ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); break;
			            case 1: ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldarg_1); break;
			            case 2: ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldarg_2); break;
			            case 3: ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldarg_3); break;
			            default:
				            if (argIndex <= 255)
					            ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldarg_S, (byte)argIndex);
				            else
					            ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldarg, argIndex);
				            break;
		            }
	            } else if (ctx.GlobalFields.ContainsKey(varName)) {
                    // Load global field
                    ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldsfld, ctx.GlobalFields[varName]);
                } else {
                    throw new Exception($"Variable '{varName}' not found in current scope");
                }
                break;
                
            default:
                throw new Exception($"Unsupported primary value type: {this.value.GetType()}");
        }
    }
}
