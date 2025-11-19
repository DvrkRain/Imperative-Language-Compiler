using Data.Objects;
using Data.ErrorHandling;
using SemanticAnalyzer.SymbolTable;

using CodeGen;
using System;
using System.Reflection;
using System.Reflection.Emit;

using SystemType = System.Type;

namespace AST;
public class AssignmentNode : Node {

	public AssignmentNode(Position pos, FieldAccessNode identifier) : base(pos) =>
		this.childs.Add(identifier);

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "AssignmentNode") Console.WriteLine($"AssignmentNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Expression
		Token token = tokenQueue.Peek();
		ExpressionNode expr = new ExpressionNode(token.Position());
		expr.Parse(ref tokenQueue);
		this.childs.Add(expr);

		// Semicolon
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();
	}


	public override void Verify() {
		base.Verify();

		foreach(var child in childs) {
			if(child.Type() == "void") {
				ErrorHandling.Add("AssignmentNode", this.position, "Wrong type on assignment");
				return;
			}
			if(DedicatedWords.Code(child.Type()) != TokenCode.builtin_type) {
				ErrorHandling.Add("AssignmentNode", this.position, "Not built-in type in assignment");
				return;
			}
		}
	}
    
	
    public override void Generate(CodeGen.CodeGenContext ctx)
    {
        var target = (FieldAccessNode)this.childs[0];
        string baseName = (string)((PrimaryNode)target.GetChilds()[0]).value;
        
        // Simple variable assignment (no indexing/field access)
        if (target.GetChilds().Count == 1)
        {
            // Generate right-hand side
            this.childs[1].Generate(ctx);
            
            // Store to variable
            if (ctx.LocalVariables.ContainsKey(baseName))
                ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Stloc, ctx.LocalVariables[baseName]);
            else if (ctx.GlobalFields.ContainsKey(baseName))
                ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Stsfld, ctx.GlobalFields[baseName]);
            else if (ctx.ParameterIndices.ContainsKey(baseName))
            {
                int argIndex = ctx.ParameterIndices[baseName];
                if (argIndex <= 255)
                    ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Starg_S, (byte)argIndex);
                else
                    ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Starg, argIndex);
            }
        }
        else
        {
            // Complex assignment: arr[i] := value or record.field := value
            GenerateComplexAssignment(ctx, target);
        }
    }

    private void GenerateComplexAssignment(CodeGen.CodeGenContext ctx, FieldAccessNode target)
    {
        string baseName = (string)((PrimaryNode)target.GetChilds()[0]).value;
        
        // Load base variable address
        SystemType currentType = null;
        if (ctx.LocalVariables.ContainsKey(baseName))
        {
            ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldloca, ctx.LocalVariables[baseName]);
            currentType = ctx.LocalVariables[baseName].LocalType;
        }
        else if (ctx.GlobalFields.ContainsKey(baseName))
        {
            ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Ldsflda, ctx.GlobalFields[baseName]);
            currentType = ctx.GlobalFields[baseName].FieldType;
        }
        
        // Navigate to target (all but last)
        for (int i = 1; i < target.GetChilds().Count - 1; i++)
        {
            var accessNode = target.GetChilds()[i];
            
            if (accessNode is ExpressionNode indexExpr)
            {
                // Array indexing
                indexExpr.Generate(ctx);
                currentType = currentType.GetElementType();
            }
            else if (accessNode is PrimaryNode fieldNode)
            {
                // Field access
                string fieldName = (string)fieldNode.value;
                var fieldInfo = currentType.GetField(fieldName);
                currentType = fieldInfo.FieldType;
            }
        }
        
        // Last accessor determines store instruction
        var lastAccess = target.GetChilds()[target.GetChilds().Count - 1];
        
        if (lastAccess is ExpressionNode finalIndex)
        {
            // Array element assignment: arr[i] := value
            finalIndex.Generate(ctx);
            this.childs[1].Generate(ctx); // Right-hand side value
            
            SystemType elementType = currentType.GetElementType();
            EmitStoreElement(ctx.CurrentIL, elementType);
        }
        else if (lastAccess is PrimaryNode finalField)
        {
            // Record field assignment: record.field := value
            string fieldName = (string)finalField.value;
            var fieldInfo = currentType.GetField(fieldName);
            
            this.childs[1].Generate(ctx);
            ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Stfld, fieldInfo);
        }
    }

    private void EmitStoreElement(System.Reflection.Emit.ILGenerator il, SystemType elementType)
    {
        if (elementType == typeof(int))
            il.Emit(System.Reflection.Emit.OpCodes.Stelem_I4);
        else if (elementType == typeof(double))
            il.Emit(System.Reflection.Emit.OpCodes.Stelem_R8);
        else if (elementType == typeof(bool))
            il.Emit(System.Reflection.Emit.OpCodes.Stelem_I1);
        else if (!elementType.IsValueType)
            il.Emit(System.Reflection.Emit.OpCodes.Stelem_Ref);
        else
            il.Emit(System.Reflection.Emit.OpCodes.Stelem, elementType);
    }

}
