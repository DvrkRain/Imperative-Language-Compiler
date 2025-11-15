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
        base.Verify();
        
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
    }
}
