using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class TypeNode : Node {
	
	public TypeNode(Position pos) : base(pos) { }

	public override void Parse(ref Queue<Token> tokenQueue) {
		// Identifier
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
		tokenQueue.Dequeue();

		// 'is' keyword
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.is_assignment) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();

		// Type
		token = tokenQueue.Peek();
		switch(token.Code()) {
			case TokenCode.identifier:
				tokenQueue.Dequeue();
				this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
				break;

			case TokenCode.builtin_type:
				tokenQueue.Dequeue();
				this.childs.Add(new PrimaryNode(token.Position(), token.Value()));
				break;

			case TokenCode.record_declaration:
				tokenQueue.Dequeue();
				RecordNode rec = new RecordNode(token.Position());
				rec.Parse(ref tokenQueue);
				this.childs.Add(rec);
				break;

			case TokenCode.array_declaration:
				tokenQueue.Dequeue();
				ArrayNode arr = new ArrayNode(this.position);
				arr.Parse(ref tokenQueue);
				this.childs.Add(arr);
				break;

			default:
				HandleUnexpectedToken(ref tokenQueue, token.Position());
				return;
		}

		// Check if declaration ends with ';'
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
		}
		tokenQueue.Dequeue();
	}

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "TypeNode") Console.WriteLine($"TypeNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}))");
		base.PrintInfo(indent);
	}

    public override void Verify() {
        this.childs[0].Verify(); // check identifier
        // Type declaration looks as follows:
        // type `Identifier` is `Type`
        
        // `Identifier` ->  PrimaryNode
        
        // Type can be
        // - PrimitiveType (integer, real, boolean) -> PrimaryNode
        // - UserType (ArrayType, RecordType)-> ArrayNode, RecordNode
        // - Identifier -> PrimaryNode
        
        // Thus, we expect childs to be
        // PrimaryNode + (PrimaryNode/ArrayNode/RecordNode)

        if (this.childs.Count() != 2) {
            ErrorHandling.Add("TypeNode", this.position, $"Expected to have 2 childs, got {this.childs.Count()}");
            return;
        }

        if (!VerifyIdentifier() || !VerifyType()) return;
        
        PrimaryNode identifier = (PrimaryNode)this.childs[0];
        string baseType;
        Scope? recordScope = null;

        switch (this.childs[1]) {
            case PrimaryNode primaryNode:
                baseType = (string)primaryNode.value;
                this.childs[1].Verify();
                break;
            
            case ArrayNode:
                baseType = "array";
                this.childs[1].Verify();
                break;
            
            case RecordNode:
                baseType = "record";
                SymbolTable.EnterScope(ScopeType.Record);
                this.childs[1].Verify();
                recordScope = SymbolTable.GetCurrentScope();
                SymbolTable.ExitScope();
                recordScope.Parent = null;
                break;
            
            default:
                baseType = (string)identifier.value;
                this.childs[1].Verify();
                break;
        }

        SemanticAnalyzer.SymbolTable.Type newType =
            new SemanticAnalyzer.SymbolTable.Type((string)identifier.value, baseType);

        if (baseType == "record") newType.TypeScope = recordScope;

        SymbolTable.DeclareEntry(newType);
    }

    private bool VerifyIdentifier() {
        // Check identifier node type
        if (this.childs[0] is not PrimaryNode) {
            ErrorHandling.Add("TypeNode", this.position, "Expected PrimaryNode");
            return false;
        }
        
        PrimaryNode identifier = (PrimaryNode)this.childs[0];
        
        // Check for redeclaration
        if (SymbolTable.FindEntry((string)identifier.value, true) != null) {
            ErrorHandling.Add("TypeNode", this.position, "Type redeclaration");
            return false;
        }

        return true;
    }

    private bool VerifyType() {
        // Type can be
        // - PrimitiveType (integer, real, boolean) -> PrimaryNode
        // - UserType (ArrayType, RecordType)-> ArrayNode, RecordNode
        // - Identifier -> PrimaryNode
        
        // Check child type
        switch (this.childs[1]) {
            case PrimaryNode primaryNode:
                if (SymbolTable.FindEntry((string)primaryNode.value) is not SemanticAnalyzer.SymbolTable.Type) {
                    ErrorHandling.Add("TypeNode", this.position, $"Type's `Type` is not declared, got {(string)primaryNode.value}");
                    return false;
                }
                break;
            
            case ArrayNode arrayNode:
                int arrayChilds = arrayNode.GetChilds().Count();
                PrimaryNode arrayType = (PrimaryNode)arrayNode.GetChilds()[arrayChilds - 1];

                if (SymbolTable.FindEntry((string)arrayType.value) is not SemanticAnalyzer.SymbolTable.Type) {
                    ErrorHandling.Add("TypeNode", this.position, $"Type's array type is not declared, got {(string)arrayType.value}");
                    return false;
                }
                break;
            
            case RecordNode recordNode:
                // TODO: Create a way to check if record is alright
                break;
            default:
                ErrorHandling.Add("TypeNode", this.position, $"Type is not expected type, got {this.childs[1].GetType().Name}");
                return false;
        }
        
        return true;
    }
}
