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
		this._type = this.childs[0].Type();

		bool flag = false;
		foreach(var child in childs) {
			if(child.Type() == "void") {
				ErrorHandling.Add("AssignmentNode", this.position, "Wrong type on assignment.");
				return;
			}
			if(DedicatedWords.Code(child.Type()) != TokenCode.builtin_type)
				flag = true;
		}

		if(flag && this._type != this.childs[1].Type()) {
			ErrorHandling.Add("AssignmentNode", this.position, "Non-built-in types on assignment cannot be casted to each other.");
			return;
		}

		if(DedicatedWords.BuiltInStrict(this._type)
			&& this.childs[1] is ExpressionNode expr
			&& this.childs[0].GetChilds() is List<Node> targetChilds
			&& targetChilds.Count() == 1
			&& targetChilds[0] is PrimaryNode target
			&& SymbolTable.FindEntry(target.Name()) is Variable var) {
			if(expr.Value() is PrimaryNode prime) {
				var.Value = this.cast(var.Type, prime.value);
				prime.value = this.cast(var.Type, prime.value);
			} else var.Value = null;
		}
		if(DedicatedWords.BuiltInStrict(this._type)
			&& this.childs[1] is ExpressionNode exp
			&& exp.Value() is PrimaryNode val)
			val.value = this.cast(this._type, val.value);

	}
    
	
    public override void Generate(CodeGen.CodeGenContext ctx) {
		base.Generate(ctx);

		FieldAccessNode target = (FieldAccessNode)this.childs[0];
		if(target.GetChilds().Count() == 1)
			ctx.CurrentIL.Emit(OpCodes.Stloc, target.variable.LocalIndex);
		else if(target.GetChilds().Last() is PrimaryNode)
			ctx.CurrentIL.Emit(OpCodes.Stfld, target.fieldInfo);
		else if(target.GetChilds().Last() is ExpressionNode)
			ctx.CurrentIL.Emit(OpCodes.Stelem_Ref);
    }


	protected object cast(string type, object value) {
		switch(type) {
			case "integer":
				switch(value) {
					case int val:
						return val;

					case float val:
						return (int)Math.Round(val, MidpointRounding.AwayFromZero);

					case bool val:
						return val?1:0;

					default: break;
				}
				break;

			case "real":
				switch(value) {
					case int val:
						return (float)val;

					case float val:
						return val;

					case bool val:
						return val?1f:0f;

					default: break;
				}
				break;

			case "boolean":
				switch(value) {
					case int val:
						if(val == 1) return true;
						else if(val == 0) return false;
						else {
							ErrorHandling.Add("Assignment", this.position, "Conversion from int to bool possible only if int is 1 or 0");
							return false;
						}

					case float val:
						ErrorHandling.Add("Assignment", this.position, "Conversion from real to bool is prohibited");
						return (int)Math.Round(val, MidpointRounding.AwayFromZero);

					case bool val:
						return val;

					default: break;
				}
				break;

			default: break;
		}
		return 0;
	}
}
