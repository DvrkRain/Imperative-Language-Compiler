using LexicalAnalyzer.TokenTree;
using LexicalAnalyzer;

namespace SyntaxAnalyzer {

    public abstract class Node {
        protected List<Node> childs;

        public Node() => this.childs = new List<Node>();

        public abstract void Parse(ref Queue<Token> tokenQueue);
    }

    public class ProgramNode : Node {
        public bool main;

        public ProgramNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) { }
    }

    public class IfNode : Node {
        public List<BranchNode> branches;

        public IfNode() : base() => this.branches = new List<BranchNode>();

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public abstract class IdentifierNode : Node {
        protected string identifier;

        public IdentifierNode() : base() => this.identifier = "";
    }

    public class VarNode : IdentifierNode {
		protected string type;
		protected bool explicit_type;

        public VarNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {
			int step = 0;
			while(step<6) {
				Token token = tokenQueue.Dequeue();
				switch(step) {
					case(0):
						switch(token) {
							case(Identifier):
								this.identifier = token.getIdentifier();
								step = 1;
								break;

							case(Mock):
								break;

							case(_):
								// Unexpected token
								break;
						}
						break;

					case(1):
						switch(token) {
							case(Dedicated):
								DedicatedWord code = token.getCode();
								switch(code) {
									case(DedicatedWord.type_assignment):
										this.explicit_type = true;
										step = 2;
										break;

									case(DedicatedWord.is_assignment):
										this.explicit_type = false;
										step = 4;
										break;

									case(_):
										// Unexpected dedicated word
										break;
								}
								break;

							case(Mock):
								break;

							case(_):
								// Unexpected token
								break;
						}
						break;

					case(2):
						switch(token) {
							case(Identifier):
								this.type = token.getIdentifier();
								step = 3;
								break;

							case(Mock):
								break;

							case(_):
								// Unexpecred token
								break;
						}
						break;

					case(3):
						switch(token) {
							case(Dedicated):
								DedicatedCode code = token.getCode();
								switch(code) {
									case(DedicatedWord.is_assignment):
										step = 4;
										break;

									case(DedicatedWord.end_of_line):
										step = 6;
										break;
								}
								break;

							case(Mock):
								break;

							case(_):
								// Unexpected token
								break;
						}
						break;

					case(4):
						ExpressionNode expr = new ExpressionNode();
						expr.Parse(ref tokenQueue);
						this.childs.Add(expr);
						step = 5;
						break;

					case(5):
						switch(token) {
							case(Dedicated):
								DedicatedWord code = token.getCode();
								if(code is DedicatedWord.end_of_line) {
									step = 6;
									break;
								} else {
									// Unexpected token
								}
								break;

							case(Mock):
								break;

							case(_):
								// Unexpected token
								break;
						}
						break;

					case(_):
						break;
				}
			}
		} 
    }

    public class TypeNode : IdentifierNode {
        public TypeNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class RecordNode : IdentifierNode {
        public RecordNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class ArrayNode : IdentifierNode {
        public ArrayNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class AssignmentNode : IdentifierNode {
        public AssignmentNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public abstract class SubprogramNode : Node {
        protected ProgramNode nested;

        public SubprogramNode() : base() {}
    }

    public class ForNode : SubprogramNode {
        public ForNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class WhileNode : SubprogramNode {
        public WhileNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class BranchNode : SubprogramNode {
        public BranchNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

	public class ExpressionNode : Node {
		public ExpressionNode() : base() { }

		public override void Parse(ref Queue<Token> tokenQueue) {}
	}
}
