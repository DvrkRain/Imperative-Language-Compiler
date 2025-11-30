using Data.Objects;
using Data.ErrorHandling;
using System.Reflection;
using System.Reflection.Emit;

using Type = Compiler.Type;
using SystemType = System.Type;

namespace Compiler.AST;
public class FieldAccessNode : Node {
	protected object value;
	public LocalBuilder variable;
	public FieldInfo fieldInfo;
	public SystemType elemInfo;

	public FieldAccessNode(Position pos) : base(pos) => this.value = "";
	public FieldAccessNode(Position pos, Node init) : this(pos) => this.childs.Add(init);

	public override void PrintInfo(string indent) {
		Console.WriteLine($"FieldAccessNode(pos={this.position.ToString()})");
		base.PrintInfo(indent);
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		Token token = tokenQueue.Peek();
		while(true) {
			while(token.Code() == TokenCode.dot) {
				tokenQueue.Dequeue();
				token = tokenQueue.Peek();
				if(token.Code() != TokenCode.identifier) {
					HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "identifier");
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
					HandleUnexpectedToken(ref tokenQueue, token.Position(), token.Code(), "right bracket");
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
				if(var.Value != null)
					this.value = var.Value;
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
				if(type.TypeScope != null) this.value = type.TypeScope;
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
		if(this.value is Scope scope) {
			switch(scope.LookupEntry(prime.Name())) {
				case Variable vr:
					this._type = vr.Type;
					if(SymbolTable.FindEntry(this._type) is Type type) {
						this.value = type.TypeScope;
					} else ErrorHandling.Add("Assignment", this.position, "Undeclared type or identifier is not a type alias");
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
		if(this.value is Scope scope) {
			this._type = (string)((Variable)scope.LookupEntry("type")).Value;
			this._type = this._type == null ? "void" : this._type;
			switch(SymbolTable.FindEntry(this._type)) {
				case Type type:
					if(type.TypeScope != null)
						this.value = type.TypeScope;
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

   
    public override void Generate(CodeGen.CodeGenContext ctx) {
        // Load base object/variable
        string baseName = ((PrimaryNode)this.childs[0]).Name();
        System.Type currentType;
        
        if (ctx.LocalVariables.ContainsKey(baseName)) {
	        this.variable = ctx.LocalVariables[baseName];
	        currentType = this.variable.LocalType;
	        if(this.childs.Count() > 1)
		        ctx.CurrentIL.Emit(OpCodes.Ldloc, this.variable);
	        
        } else if (ctx.GlobalFields.ContainsKey(baseName)) {
	        this.fieldInfo = ctx.GlobalFields[baseName];
	        currentType = this.fieldInfo.FieldType;
	        if (this.childs.Count() > 1)
		        ctx.CurrentIL.Emit(OpCodes.Ldsfld, this.fieldInfo);

        } else throw new Exception($"Undeclared variable {baseName}");

		for(int i=1; i<this.childs.Count(); i++) {
			var accessNode = this.childs[i];
			if(accessNode is ExpressionNode index) {
				// Array is already loaded
				index.Generate(ctx); // Load array index
				ctx.CurrentIL.Emit(OpCodes.Ldc_I4_1);
				ctx.CurrentIL.Emit(OpCodes.Sub);
				if (ctx.UserTypes.ContainsKey(currentType.FullName))
					currentType = currentType.GetField("data").FieldType;
				
				this.elemInfo = currentType.GetElementType();
				currentType = this.elemInfo;
				
				if(i != this.childs.Count() - 1)
					ctx.CurrentIL.Emit(OpCodes.Ldelem, currentType);
				
			} else if(accessNode is PrimaryNode field) {
                this.fieldInfo = currentType.GetField(field.Name());
				currentType = fieldInfo.FieldType;
				if(i != this.childs.Count() - 1)
					ctx.CurrentIL.Emit(OpCodes.Ldfld, this.fieldInfo);
			}
		}
    }
}
