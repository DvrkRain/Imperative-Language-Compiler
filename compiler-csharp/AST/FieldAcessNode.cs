using Data.Objects;
using SemanticAnalyzer.SymbolTable;

using CodeGen;
using System;
using System.Reflection;
using System.Reflection.Emit;

using SystemType = System.Type;

namespace AST;
public class FieldAccessNode : Node {
	private int depth;
	public FieldAccessNode(Position pos) : base(pos) => this.depth = 0;
	public FieldAccessNode(Position pos, Node init) : this(pos) =>
		this.childs.Add(init);

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "FieldAccessNode") Console.WriteLine($"FieldAccessNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		Token token = tokenQueue.Peek();
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
			this.depth++;
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
