using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class ParameterNode : Node {
	public string Name() {
		if(this.childs[0] is PrimaryNode prime) return prime.Name();
		return "unknown";
	}

	public new string Type() {
		return this._type;
	}

	public ParameterNode(Position pos) : base(pos) { }

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "ParameterNode") Console.WriteLine($"ParameterNode(childs={this.childs.Count}, , pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}
	

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


    public override void Verify() {
        base.Verify();

        if (this.childs.Count() != 2) {
            ErrorHandling.Add("ParameterNode", this.position, $"Expected 2 childs, got {this.childs.Count()}");
            return;
        }

        switch (this.childs[1]) {
            case PrimaryNode primaryNode:
                this._type = primaryNode.Name();
                break;

            case ArrayNode arrayNode:
                this._type = $"_array{arrayNode.Type()}"; //i.e. %integerarray
                Scope arrayScope = new Scope();
                arrayScope.AddEntry(new Variable("size", "integer"));
                arrayScope.AddEntry(new Variable("type", "void", arrayNode.Type()));
                SemanticAnalyzer.SymbolTable.Type newArrayType = new SemanticAnalyzer.SymbolTable.Type(this._type, "array");
                newArrayType.TypeScope = arrayScope;
                SymbolTable.DeclareEntry(newArrayType);
                break;

            default:
                ErrorHandling.Add("ParameterNode", this.position,
						$"Expected parameter type as PrimaryNode or ArrayNode, got {this.childs[1].GetType().Name}");
                return;
        }

        if (SymbolTable.FindEntry(this._type) is not SemanticAnalyzer.SymbolTable.Type) {
            ErrorHandling.Add("ParameterNode", this.position, $"Parameter type not declared, got {this._type}");
            return;
        }

        SemanticAnalyzer.SymbolTable.Type paramType = (SemanticAnalyzer.SymbolTable.Type)SymbolTable.FindEntry(this._type);

        Scope? typeScope = paramType.BaseType == "record" ? paramType.TypeScope : null;
        Variable variable = new Variable(((PrimaryNode)this.childs[0]).Name(), this._type, typeScope);
        SymbolTable.DeclareEntry(variable);
    }

}
