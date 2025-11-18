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
        else if (token.Code() == TokenCode.array_declaration) {
            ArrayNode arrayNode = new ArrayNode(token.Position());
            arrayNode.Parse(ref tokenQueue);
            this.childs.Add(arrayNode);
        } else HandleUnexpectedToken(ref tokenQueue, token.Position());
    }

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ParameterNode") Console.WriteLine($"ParameterNode(childs={this.childs.Count}, , pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}

    public override void Verify() {
        base.Verify();

        if (!VerifyType()) return;

        string paramName = (string)((PrimaryNode)this.childs[0]).value;
        string paramTypeName;
        
        switch (this.childs[1]) {
            case PrimaryNode primaryNode:
                paramTypeName = (string)primaryNode.value;
                break;
            case ArrayNode arrayNode:
                paramTypeName = (string)((PrimaryNode)arrayNode.GetChilds().Last()).value;
                break;
            default:
                ErrorHandling.Add("ParameterNode", this.position, $"Expected parameter type as PrimaryNode or ArrayNode, got {this.childs[1].GetType().Name}");
                return;
        }

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

        string parameterType = "void";

        // First child - identifier
        // Second child - type

        switch (this.childs[1]) {
            case PrimaryNode primaryNode:
                parameterType = (string)primaryNode.value;
                break;
            case ArrayNode arrayNode:
                parameterType = (string)((PrimaryNode)arrayNode.GetChilds().Last()).value;
                break;
            default:
                ErrorHandling.Add("ParameterNode", this.position, $"Expected parameter type as PrimaryNode or ArrayNode, got {this.childs[1].GetType().Name}");
                return false;
        }

        if (SymbolTable.FindEntry(parameterType) is not SemanticAnalyzer.SymbolTable.Type) {
            ErrorHandling.Add("ParameterNode", this.position, $"Parameter type not declared, got {parameterType}");
            return false;
        }

        return true;
    }

    public override void Unuse() {
    }
}
