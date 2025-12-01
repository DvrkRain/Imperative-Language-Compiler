using Data.ErrorHandling;
using Data.Objects;
using System.Reflection;
using System.Reflection.Emit;

using SystemType = System.Type;

namespace Compiler.AST;
public class VarNode : Node {
	protected bool explicit_type;

	public VarNode(Position pos) : base(pos) {
		this.explicit_type = false;
		this._type = "void";
	}

	public override void PrintInfo(string indent) {
		Console.WriteLine($"VarNode(pos={this.position.ToString()}, type={this._type}, explicit={this.explicit_type})");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Search for identifier
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "identifier");
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
				HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "type identifier");
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
			HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "initialization without explicit type");
			return;
		}

		// Check if declaration ends with ';'
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "semicolon");
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

			case Type type:
				if(DedicatedWords.Code(type.BaseType) == TokenCode.builtin_type)
					this._type = type.BaseType;
				break;

			default:
				ErrorHandling.Add("VarNode", this.position, $"Expected type identifier");
				return;
		}

		// Expression (if present)
		if(this.childs.Count() > 1) {
			this.childs[1] = ((ExpressionNode)this.childs[1]).Value();
			if(!this.explicit_type) this._type = this.childs[1].Type();
			if(this.childs[1] is PrimaryNode prime)
				val = prime.value;
		}
		SymbolTable.DeclareEntry(new Variable(identifier.Name(), this._type, val));
    }
    
    public override void Generate(CodeGen.CodeGenContext ctx) {
        string varName = ((PrimaryNode)this.childs[0]).Name();
        SystemType varType = ctx.ResolveType(this._type);
        
		// Global variable
		if (ctx.CurrentMethod.Name == "_Main") {
			ctx.GlobalFields[varName] = ctx.ProgramTypeBuilder.DefineField(varName, varType, 
				FieldAttributes.Public | FieldAttributes.Static);
		} else {
			// Local variable
			ctx.LocalVariables[varName] = ctx.CurrentIL.DeclareLocal(varType);
		}

		if (this.childs.Count > 1)
		{
			// Initialize with expression
			this.childs[1].Generate(ctx);
			if (ctx.LocalVariables.ContainsKey(varName))
				ctx.CurrentIL.Emit(OpCodes.Stloc, ctx.LocalVariables[varName]);
			else
				ctx.CurrentIL.Emit(OpCodes.Stsfld, ctx.GlobalFields[varName]);
		}
		else if (ctx.UserTypes.ContainsKey(varType.Name)) {
			varType = ctx.UserTypes[varType.Name];
			ctx.CurrentIL.Emit(OpCodes.Newobj, varType.GetConstructor(System.Type.EmptyTypes));
			if (ctx.LocalVariables.ContainsKey(varName))
				ctx.CurrentIL.Emit(OpCodes.Stloc, ctx.LocalVariables[varName]);
			else
				ctx.CurrentIL.Emit(OpCodes.Stsfld, ctx.GlobalFields[varName]);
		}
		else if (ctx.ArrayTypes.ContainsKey(this._type)) {
			// Put array size into stack
			if (ctx.LocalVariables.ContainsKey(this._type)) ctx.CurrentIL.Emit(OpCodes.Ldloc, ctx.LocalVariables[this._type]);
			else ctx.CurrentIL.Emit(OpCodes.Ldsfld, ctx.GlobalFields[this._type]);
			
			// Call newarr with requested type
			ctx.CurrentIL.Emit(OpCodes.Newarr, varType);
			
			// Store into variable
			if (ctx.LocalVariables.ContainsKey(varName)) ctx.CurrentIL.Emit(OpCodes.Stloc, ctx.LocalVariables[varName]);
			else  ctx.CurrentIL.Emit(OpCodes.Stsfld, ctx.GlobalFields[varName]);
		}
    }

}
