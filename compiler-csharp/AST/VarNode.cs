using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

using CodeGen;
using System;
using System.Reflection;
using System.Reflection.Emit;

using SystemType = System.Type;

namespace AST;
public class VarNode : Node {
	protected bool explicit_type;

	public VarNode(Position pos) : base(pos) {
		this.explicit_type = false;
		this._type = "void";
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "VarNode") Console.WriteLine($"VarNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}), type={this._type}, explicit={this.explicit_type})");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Search for identifier
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();
		this.childs.Add(new PrimaryNode(token.Position(), token.Value()));

		// Check if type is explicitly stated
		bool flag = false;
		token = tokenQueue.Peek();
		if(token.Code() == TokenCode.type_assignment) {
			tokenQueue.Dequeue();
			flag = true;
			this.explicit_type = true;
			token = tokenQueue.Peek();
			if(token.Code() == TokenCode.builtin_type || token.Code() == TokenCode.identifier) {
				this._type = (string)token.Value();
			} else {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
			}
			tokenQueue.Dequeue();
			token = tokenQueue.Peek();
			if(token.Code() == TokenCode.semicolon) {
				tokenQueue.Dequeue();
				return;
			}
		}

		// Check if variable initialized in declaration
		if(token.Code() == TokenCode.is_assignment) {
			tokenQueue.Dequeue();
			ExpressionNode expr = new ExpressionNode(token.Position());
			expr.Parse(ref tokenQueue);
			this.childs.Add(expr);
		} else if (!flag) {
			tokenQueue.Dequeue();
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}

		// Check if declaration ends with ';'
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();
	} 


    public override void Verify() {
        base.Verify();

        // Check child type
        if (this.childs[0] is PrimaryNode identifier) {
			// Check if identifier already exists
			if (SymbolTable.FindEntry(identifier.Name(), true) != null) {
				ErrorHandling.Add("VarNode", this.position, $"Identifier '{identifier.Name()}' already exists");
				return;
			}
        } else {
            ErrorHandling.Add("VarNode", this.position, "Expected PrimaryNode");
            return;
		}
                
        // Special void check
        if (this._type != "void" && !this.explicit_type) {
			ErrorHandling.Add("VarNode", this.position, "Variable declared without both expression and explicit type");
            return;
        }

		// Check type
		object? val=null;
		switch(SymbolTable.FindEntry(this._type)) {
			case null:
				if (this.explicit_type) {
					ErrorHandling.Add("VarNode", this.position, "Variable type not declared");
					return;
				}
				break;

			case SemanticAnalyzer.SymbolTable.Type type:
				if(DedicatedWords.Code(type.BaseType) == TokenCode.builtin_type)
					this._type = type.BaseType;
				val = type.TypeScope;
				break;

			default:
				ErrorHandling.Add("VarNode", this.position, $"Expected type identifier");
				return;
		}

		// Expression (if present)
		if(this.childs.Count() > 1) {
		this.childs[1] = ((ExpressionNode)this.childs[1]).Value();
		if(!this.explicit_type) this._type = this.childs[1].Type();
		switch(this.childs[1]) {
			case PrimaryNode prime:
				val = prime.value;
				break;

			case ExpressionNode expr:
				break;

			default: break;
		}
		}

		SymbolTable.DeclareEntry(new Variable(identifier.Name(), this._type, val));
    }
    
    public override void Generate(CodeGenContext ctx)
    {
        string varName = ((PrimaryNode)this.childs[0]).Name();
        SystemType varType = ctx.ResolveType(this._type);
        
        // Check if this is an array type
        bool isArray = varType.IsArray || ctx.GetArraySize(this._type) > 0;
        int arraySize = ctx.GetArraySize(this._type);
    
		// Local variable
		var local = ctx.CurrentIL.DeclareLocal(varType);
		ctx.LocalVariables[varName] = local;
	
		// Initialize array if needed
		if (isArray && arraySize > 0)
		{
			// Create array: newarr elementType
			SystemType elementType = varType.GetElementType();
			CodeGen.ILHelper.EmitLoadInt(ctx.CurrentIL, arraySize);
			ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Newarr, elementType);
			ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Stloc, local);
		}
		else if (this.childs.Count > 1)
		{
			// Initialize with expression
			this.childs[1].Generate(ctx);
			ctx.CurrentIL.Emit(System.Reflection.Emit.OpCodes.Stloc, local);
		}
		else if (ctx.UserTypes.ContainsKey(varType.Name)) {
			varType = ctx.UserTypes[varType.Name];
			ctx.CurrentIL.Emit(OpCodes.Newobj, varType.GetConstructor(System.Type.EmptyTypes));
			ctx.CurrentIL.Emit(OpCodes.Stloc, local);
		}
    }

}
