using Compiler.Data;

namespace Compiler.AST;
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
		Console.WriteLine($"PrimaryNode(pos={this.position.ToString()}, value={this.value})");
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
					}
				break;

			case Variable var:
				this._type = var.Type;
				if(SymbolTable.IsInsideType(ScopeType.Loop)) return;
				this.value = var.Value ?? "unknown";
				break;

			case int:
				this._type = "integer";
				break;

			case float:
				this._type = "real";
				break;

			case bool:
				this._type = "boolean";
				break;
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
		            int argIndex = ctx.ParameterIndices[varName];
		            ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldarg, argIndex);
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
