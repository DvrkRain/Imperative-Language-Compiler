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
	protected object value;
	public LocalBuilder variable;
	public FieldInfo fieldInfo;

	public FieldAccessNode(Position pos) : base(pos) =>
	   this.value = "";

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
					this.value = vr.Value;
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
		var variable = ctx.LocalVariables[baseName];
		var currentType = variable.LocalType;
		FieldInfo fieldInfo = null;
		if(this.childs.Count() > 1)
			ctx.CurrentIL.Emit(OpCodes.Ldloca, variable);

		for(int i=1; i<this.childs.Count(); i++) {
			var accessNode = this.childs[i];
			if(accessNode is ExpressionNode index) {
			} else if(accessNode is PrimaryNode field) {
                string fieldName = field.Name();
                fieldInfo = currentType.GetField(fieldName);
				ctx.CurrentIL.Emit(OpCodes.Ldflda, fieldInfo);
			}
		}
        
        // Load base variable
        // if (ctx.ParameterIndices.ContainsKey(baseName)) {
        //     int argIndex = ctx.ParameterIndices[baseName];
        //     currentType = ctx.ParameterTypes[baseName];
        //
        // } else if (ctx.LocalVariables.ContainsKey(baseName)) {
		// }
        
            // if (accessNode is ExpressionNode indexExpr) {
            //     // Array indexing: arr[index]
            //     if (currentType.IsArray) {
            //         // Generate index expression
            //         indexExpr.Generate(ctx);
            //
            //         // Load element: ldelem
            //         SystemType elementType = currentType.GetElementType();
            //         EmitLoadElement(ctx.CurrentIL, elementType);
            //         currentType = elementType;
            //     }
    }
}
