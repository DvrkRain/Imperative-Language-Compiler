using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;

using CodeGen;
using System;
using System.Reflection;
using System.Reflection.Emit;

using Type = SemanticAnalyzer.SymbolTable.Type;
using SystemType = System.Type;

namespace AST;
public class FieldAccessNode : Node {
	protected object variable;
	public object Variable() =>
		this.variable;

	public FieldAccessNode(Position pos) : base(pos) =>
	   this.variable = "";

	public FieldAccessNode(Position pos, Node init) : this(pos) =>
		this.childs.Add(init);

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "FieldAccessNode") Console.WriteLine($"FieldAccessNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		Token token = tokenQueue.Peek();
		while(true) {
			while(token.Code() == TokenCode.dot) {
				tokenQueue.Dequeue();
				token = tokenQueue.Peek();
				if(token.Code() != TokenCode.identifier) {
					HandleUnexpectedToken(ref tokenQueue, token.Position());
					return;
				}
				tokenQueue.Dequeue();
				this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
				token = tokenQueue.Peek();
			}
			while(token.Code() == TokenCode.left_bracket) {
				tokenQueue.Dequeue();
				ExpressionNode expr = new ExpressionNode(tokenQueue.Peek().Position(), true);
				expr.Parse(ref tokenQueue);
				this.childs.Add(expr);
				token = tokenQueue.Peek();
				if(token.Code() != TokenCode.right_bracket) {
					HandleUnexpectedToken(ref tokenQueue, token.Position());
					return;
				}
				tokenQueue.Dequeue();
				token = tokenQueue.Peek();
			}
			if(token.Code() != TokenCode.dot && token.Code() != TokenCode.left_bracket) break;
		}
	}

	
	public override void Verify() {
		base.Verify();

		bool flag = false;
		if(this.childs[0] is PrimaryNode primary) {
		switch(SymbolTable.FindEntry(primary.Name())) {
			case Variable var:
				this._type = var.Type;
				this.variable = var;
				break;

			case null:
				ErrorHandling.Add("FieldAccessNode", primary.Position(), "Undeclared variable.");
				return;

			default:
				ErrorHandling.Add("FieldAccessNode", primary.Position(), "Expected variable identifier.");
				return;
		}
		}

		switch(SymbolTable.FindEntry(this._type)) {
			case Type type:
				if(type.TypeScope != null) this.variable = type.TypeScope;
				break;

			case null:
				ErrorHandling.Add("FieldAccessNode", this.Position(), "Expected type identifier.");
				break;

			default:
				ErrorHandling.Add("FieldAccessNode", this.Position(), "Undeclared type.");
				break;
		}

		for(int i=1; i<this.childs.Count(); i++) {
		if(this.childs[i] is PrimaryNode prime) {
		if(this.variable is Scope scope) {
			switch(scope.LookupEntry(prime.Name())) {
				case Variable vr:
					this._type = vr.Type;
					this.variable = vr.Value;
					break;

				case null:
					ErrorHandling.Add("FieldAccessNode", prime.Position(), $"Record {this._type} does not have field {prime.Name()}.");
					break;

				default:
					ErrorHandling.Add("FieldAccessNode", prime.Position(), "Expected variable identifier.");
					break;
			}
		} else {
			ErrorHandling.Add("FieldAccessNode", prime.Position(), "Cannot access member");
			return;
		}

		} else if(this.childs[i] is ExpressionNode expr) {
		flag = true;
		if(expr.Type() != "integer") {
			ErrorHandling.Add("FieldAccessNode", this.position, "Array index should be of type integer");
			return;
		}
		if(this.variable is Scope scope) {
			this._type = (string)((Variable)scope.LookupEntry("type")).Value;
			this._type = this._type == null ? "void" : this._type;
			switch(SymbolTable.FindEntry(this._type)) {
				case Type type:
					if(type.TypeScope != null)
						this.variable = type.TypeScope;
					break;

				case null:
					ErrorHandling.Add("FieldAccessNode", expr.Position(), $"Undeclared type {this._type}.");
					return;

				default:
					ErrorHandling.Add("FieldAccessNode", expr.Position(), "Expected type identifier");
					return;
			}
		} else {
			ErrorHandling.Add("FieldAccessNode", expr.Position(), "Cannot access member");
			return;
		}
		}
		}
	}

    
    public override void Generate(CodeGen.CodeGenContext ctx)
    {
        // Load base object/variable
        string baseName = (string)((PrimaryNode)this.childs[0]).value;
        
        // Determine if it's parameter, local, or global
        bool isParameter = ctx.ParameterIndices.ContainsKey(baseName);
        bool isLocal = ctx.LocalVariables.ContainsKey(baseName);
        bool isGlobal = ctx.GlobalFields.ContainsKey(baseName);
        
        SystemType currentType = null;
        
        // Load base variable
        if (isParameter)
        {
            int argIndex = ctx.ParameterIndices[baseName];
            EmitLoadArg(ctx.CurrentIL, argIndex);
            currentType = ctx.ParameterTypes[baseName];
        }
        else if (isLocal)
        {
            var local = ctx.LocalVariables[baseName];
            ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldloc, local);
            currentType = local.LocalType;
        }
        else if (isGlobal)
        {
            var field = ctx.GlobalFields[baseName];
            ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldsfld, field);
            currentType = field.FieldType;
        }
        
        // Process chain of accesses
        for (int i = 1; i < this.childs.Count; i++)
        {
            var accessNode = this.childs[i];
            
            if (accessNode is ExpressionNode indexExpr)
            {
                // Array indexing: arr[index]
                if (currentType.IsArray)
                {
                    // Generate index expression
                    indexExpr.Generate(ctx);
                    
                    // Load element: ldelem
                    SystemType elementType = currentType.GetElementType();
                    EmitLoadElement(ctx.CurrentIL, elementType);
                    currentType = elementType;
                }
            }
            else if (accessNode is PrimaryNode fieldNode)
            {
                // Record field access: record.field
                string fieldName = (string)fieldNode.value;
                var fieldInfo = currentType.GetField(fieldName);
                
                if (fieldInfo != null)
                {
                    ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldfld, fieldInfo);
                    currentType = fieldInfo.FieldType;
                }
            }
        }
    }

    private void EmitLoadArg(System.Reflection.Emit.ILGenerator il, int index)
    {
        switch (index)
        {
            case 0: il.Emit(System.Reflection.Emit.OpCodes.Ldarg_0); break;
            case 1: il.Emit(System.Reflection.Emit.OpCodes.Ldarg_1); break;
            case 2: il.Emit(System.Reflection.Emit.OpCodes.Ldarg_2); break;
            case 3: il.Emit(System.Reflection.Emit.OpCodes.Ldarg_3); break;
            default:
                if (index <= 255)
                    il.Emit(System.Reflection.Emit.OpCodes.Ldarg_S, (byte)index);
                else
                    il.Emit(System.Reflection.Emit.OpCodes.Ldarg, index);
                break;
        }
    }

    private void EmitLoadElement(System.Reflection.Emit.ILGenerator il, SystemType elementType)
    {
        if (elementType == typeof(int))
            il.Emit(System.Reflection.Emit.OpCodes.Ldelem_I4);
        else if (elementType == typeof(double))
            il.Emit(System.Reflection.Emit.OpCodes.Ldelem_R8);
        else if (elementType == typeof(bool))
            il.Emit(System.Reflection.Emit.OpCodes.Ldelem_I1);
        else if (!elementType.IsValueType)
            il.Emit(System.Reflection.Emit.OpCodes.Ldelem_Ref);
        else
            il.Emit(System.Reflection.Emit.OpCodes.Ldelem, elementType);
    }
}
