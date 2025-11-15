using System.Security.Cryptography;
using Data.ErrorHandling;
using Data.Objects;
using SemanticAnalyzer.SymbolTable;

namespace AST;
public class VarNode : Node {
	protected string type;
	protected bool explicit_type;

	public VarNode(Position pos) : base(pos) {
		this.explicit_type = false;
		this.type = "void";
	}


	public override void Parse(ref Queue<Token> tokenQueue) {
		// Search for identifier
		Token token = tokenQueue.Peek();
		if(token.Code() != TokenCode.identifier) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
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
				this.type = (string)token.Value();
			} else {
				HandleUnexpectedToken(ref tokenQueue, token.Position());
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
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}

		// Check if declaration ends with ';'
		token = tokenQueue.Peek();
		if(token.Code() != TokenCode.semicolon) {
			HandleUnexpectedToken(ref tokenQueue, token.Position());
			return;
		}
		tokenQueue.Dequeue();
	} 

	public override void PrintInfo(string indent) {
		if (this.GetType().Name == "VarNode") Console.WriteLine($"VarNode(childs={this.childs.Count}, pos=({this.position.Row()}, {this.position.Col()}), type={this.type}, explicit={this.explicit_type})");
		base.PrintInfo(indent);
	}

    public override void Verify() {
        base.Verify();
        
        // Variable declaration looks as follows:
        // - var `Identifier` : `Type` [is `Expression`]
        // - var `Identifier` is `Expression`
        
        // `Identifier` -> PrimaryNode
        // `Type` -> this.type
        // `Expression` -> ExpressionNode
        
        // We expect 2 setups:
        switch (this.childs.Count()) {
            case 1: // PrimaryNode
                if (!VerifyIdentifier() || !VerifyType()) return;
                PrimaryNode primary = (PrimaryNode)this.childs[0];

                SymbolTable.DeclareEntry(new Variable((string)primary.value, this.type));
                break;
            
            case 2: // PrimaryNode + ExpressionNode
                if (!VerifyIdentifier() || !VerifyType() || !VerifyExpression()) return;
                primary = (PrimaryNode)this.childs[0];
                ExpressionNode expression = (ExpressionNode)this.childs[1];

                SymbolTable.DeclareEntry(new Variable((string)primary.value, this.type, expression));
                // TODO: put expression value if possible
                break;
            default:
                ErrorHandling.Add("VarNode", this.position, $"Expected 1 or 2 childs, got {this.childs.Count()}");
                return;
        }
    }

    private bool VerifyIdentifier() {
        // Check child type
        if (this.childs[0] is not PrimaryNode) {
            ErrorHandling.Add("VarNode", this.position, "Expected PrimaryNode");
            return false;
        }
                
        PrimaryNode primary = (PrimaryNode)this.childs[0];
                
        // Check for redeclaration 
        if (SymbolTable.FindEntry((string)primary.value, true) != null) {
            ErrorHandling.Add("VarNode", this.position, "Variable redeclaration exception");
            return false;
        }

        return true;
    }

    private bool VerifyType() {
        // Check for variable type
        if (SymbolTable.FindEntry(this.type) == null && this.type != "void") {
            ErrorHandling.Add("VarNode", this.position, "Variable type not declared");
            return false;
        }
        return true;
    }

    private bool VerifyExpression() {
        if (this.childs[1] is not ExpressionNode) {
            ErrorHandling.Add("VarNode", this.position, "Expected ExpressionNode");
            return false;
        }

        ExpressionNode expr = (ExpressionNode)this.childs[1];
        
        // Since this method called only in 2nd setup
        // We can use 1st child w/o doubts
        
        PrimaryNode primary = (PrimaryNode)this.childs[0];
                
        // TODO: Add check variable type and expression type matching

        return true;
    }
}
