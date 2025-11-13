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

    public override void Verify(ref SymbolTable symTab) {
        base.Verify(ref symTab);

        switch (this.childs.Count) {
            case 1:
                // Check child type
                if (this.childs[0] is not PrimaryNode) {
                    ErrorHandling.Add("VarNode", this.position, "Expected PrimaryNode");
                    return;
                }
                
                PrimaryNode primary = (PrimaryNode)this.childs[0];
                
                // Check for redeclaration 
                if (symTab.FindEntry((string)primary.value, true) != null) {
                    ErrorHandling.Add("VarNode", this.position, "Variable redeclaration exception");
                    return;
                }
                
                // Check for variable type
                if (symTab.FindEntry(this.type) == null) {
                    ErrorHandling.Add("VarNode", this.position, "Variable type not declared");
                    return;
                }

                Variable var = new Variable((string)primary.value, this.type);
                symTab.DeclareEntry(var);
                break;
            
            case 2:
                if (this.childs[0] is not PrimaryNode) {
                    ErrorHandling.Add("VarNode", this.position, "Expected PrimaryNode");
                    return;
                }
                
                primary = (PrimaryNode)this.childs[0];

                if (symTab.FindEntry((string)primary.value, true) != null) {
                    ErrorHandling.Add("VarNode", this.position, "Variable redeclaration exception");
                    return;
                }

                if (symTab.FindEntry(this.type) == null) {
                    ErrorHandling.Add("VarNode", this.position, "Variable type not declared");
                    return;
                }

                if (this.childs[1] is not ExpressionNode) {
                    ErrorHandling.Add("VarNode", this.position, "Expected ExpressionNode");
                    return;
                }

                ExpressionNode expr = (ExpressionNode)this.childs[1];
                break;
            default:
                ErrorHandling.Add("VarNode", this.position, $"Unexpected number of childs: {this.childs.Count}");
                break;
        }


    }
}
