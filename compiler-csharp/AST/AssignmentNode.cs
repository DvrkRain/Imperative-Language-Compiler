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

		bool flag = false;
		foreach(var child in childs) {
			if(child.Type() == "void") {
				ErrorHandling.Add("AssignmentNode", this.position, "Wrong type on assignment.");
				return;
			}
			if(DedicatedWords.Code(child.Type()) != TokenCode.builtin_type)
				flag = true;
		}

		if(flag && this.childs[0].Type() != this.childs[1].Type()) {
			ErrorHandling.Add("AssignmentNode", this.position, "Non-built-in types on assignment cannot be casted to each other.");
			return;
		}
	}
    
	
    public override void Generate(CodeGen.CodeGenContext ctx) {
		FieldAccessNode target = (FieldAccessNode)this.childs[0];
		this.childs[1].Generate(ctx);
		target.Generate(ctx);

		if(target.GetChilds().Count() == 1)
			ctx.CurrentIL.Emit(OpCodes.Stloc, target.variable.LocalIndex);
		else if(target.GetChilds().Last() is PrimaryNode)
			ctx.CurrentIL.Emit(OpCodes.Stfld, target.fieldInfo);

    }
}
