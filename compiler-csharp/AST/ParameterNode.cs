using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class ParameterNode : Node {
	public ParameterNode(Position pos) : base(pos) { }
	

	public override void Parse(ref Queue<Token> tokenQueue) {
		// Identifier
		Token token = tokenQueue.Peek();
		if(token.Code() == TokenCode.identifier) {
			this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
		} else {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Type assignment
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.type_assignment) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Type
		token = tokenQueue.Dequeue();
        
        // Record or built-in types
		if(token.Code() == TokenCode.identifier || token.Code() == TokenCode.builtin_type)
			this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
        // TODO: Add array case
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ParameterNode") Console.WriteLine($"ParameterNode(childs={this.childs.Count}, , pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}

    public override void Verify() {
        base.Verify();

        if (!VerifyType()) return;

        string paramName = (string)((PrimaryNode)this.childs[0]).value;
        string paramTypeName = (string)((PrimaryNode)this.childs[1]).value;
        SemanticAnalyzer.SymbolTable.Type paramType = (SemanticAnalyzer.SymbolTable.Type)SymbolTable.FindEntry(paramTypeName);

        Scope? typeScope = paramType.BaseType == "record" ? paramType.TypeScope : null;
        Variable variable = new Variable(paramName, paramTypeName, typeScope);
        SymbolTable.DeclareEntry(variable);
    }

    private bool VerifyType() {
        if (this.childs.Count() != 2) {
            ErrorHandling.Add("ParameterNode", this.position, $"Expected 2 childs, got {this.childs.Count()}");
            return false;
        }

        for (int i = 0; i < this.childs.Count(); i++) {
            if (this.childs[i] is not PrimaryNode) {
                ErrorHandling.Add("ParameterNode", this.position, $"Child {i} expected PrimaryNode, got {this.childs[i].GetType().Name}");
                return false;
            }
        }
        
        PrimaryNode parameterType = (PrimaryNode)this.childs[1];
        if (SymbolTable.FindEntry((string)parameterType.value) is not SemanticAnalyzer.SymbolTable.Type) {
            ErrorHandling.Add("ParameterNode", this.position, $"Parameter type not declared, got {(string)parameterType.value}");
            return false;
        }

        return true;
    }
}
