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

        public override void Parse(ref Queue<Token> tokenQueue) {
            int step = 0;
            bool parsing = true;
            
            while(parsing && tokenQueue.Count > 0) {
                Token token = tokenQueue.Peek();
                
                switch(step) {
                    case 0: // Expecting "if" keyword
                        switch(token) {
                            case Dedicated dedicated when dedicated.getCode() == DedicatedWord.if_statement:
                                tokenQueue.Dequeue(); // Consume "if"
                                step = 1;
                                break;
                            case Mock:
                                tokenQueue.Dequeue();
                                break;
                            default:
                                // Unexpected token
                                // TODO: Add exception throw
                                parsing = false;
                                break;
                        }
                        break;
                        
                    case 1: // Parse condition expression
                        ExpressionNode condition = new ExpressionNode();
                        condition.Parse(ref tokenQueue);
                        this.childs.Add(condition);

                        step = 2;
                        break;
                        
                    case 2: // Expecting "then" keyword
                        switch(token) {
                            case Dedicated dedicated when dedicated.getCode() == DedicatedWord.then_branch:
                                tokenQueue.Dequeue(); // Consume "then"
                                step = 3;
                                break;
                            case Mock:
                                tokenQueue.Dequeue();
                                break;
                            default:
                                // Unexpected token
                                // TODO: Add exception throw
                                parsing = false;
                                break;
                        }
                        break;
                        
                    case 3 or 5: // Parse branch statements until "else" or "end"
                        BranchNode newBranch = new BranchNode();
                        newBranch.Parse(ref tokenQueue);
                        this.childs.Add(newBranch);

                        step += 1;
                        break;
                        
                    case 4: // Check for "else" or "end"
                        switch(token) {
                            case Dedicated dedicated when dedicated.getCode() == DedicatedWord.else_branch:
                                tokenQueue.Dequeue(); // Consume "else"
                                step = 5;
                                break;
                                
                            case Dedicated dedicated when dedicated.getCode() == DedicatedWord.end_of_body:
                                tokenQueue.Dequeue(); // Consume "end"
                                step = 6; // Proceed to final validation
                                break;
                                
                            case Mock:
                                tokenQueue.Dequeue();
                                break;
                                
                            default:
                                // Unexpected token
                                // TODO: Add exception throw
                                parsing = false;
                                break;
                        }
                        break;
                        
                    case 6: // Expecting semicolon
                        switch(token) {
                            case Dedicated dedicated when dedicated.getCode() == DedicatedWord.end_of_line:
                                tokenQueue.Dequeue(); // Consume ";"
                                parsing = false; // Successfully completed
                                break;
                            case Mock:
                                tokenQueue.Dequeue();
                                break;
                            default:
                                // Unexpected token
                                // TODO: Add exception throw
                                parsing = false;
                                break;
                        }
                        break;
                }
            }
        }
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
								this.identifier = ((Identifier) token).getIdentifier();
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
								DedicatedWord code = ((Dedicated) token).getCode();
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
								this.type = ((Identifier) token).getIdentifier();
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
								DedicatedWord code = ((Dedicated) token).getCode();
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
								DedicatedWord code = ((Dedicated) token).getCode();
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

    public override void Parse(ref Queue<Token> tokenQueue) {
        bool parsing = true;
        
        while(parsing && tokenQueue.Count > 0) {
            Token token = tokenQueue.Peek();
            
            // We assume that "record" keyword is already consumed
            switch(token) {
                case Dedicated dedicated when dedicated.getCode() == DedicatedWord.end_of_body:
                    tokenQueue.Dequeue(); // Consume "end"
                    parsing = false;
                    break;
                case Identifier: // Field declaration starts
                    VarNode fieldNode = new VarNode();
                    fieldNode.Parse(ref tokenQueue);
                    this.childs.Add(fieldNode);
                    // Remain in step 0 to get next field or "end"
                    break;
                case Mock:
                    tokenQueue.Dequeue();
                    break;
                default:
                    // Unexpected token
                    // TODO: Add exception throw
                    parsing = false;
                    break;
            }
        }
    }
}

    public class ArrayNode : IdentifierNode {
    
    public ArrayNode() : base() {}

    public override void Parse(ref Queue<Token> tokenQueue) {
        int step = 0;
        bool parsing = true;
        
        while(parsing && tokenQueue.Count > 0) {
            Token token = tokenQueue.Peek();
            
            // We assume that "array" keyword is already consumed
            switch(step) {
                case 0: // Expecting opening bracket "["
                    switch(token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.right_bracket:
                            tokenQueue.Dequeue(); // Consume "["
                            step = 1;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token
							// TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 1: // Parse size expression
                    // Create and parse the size expression
                    ExpressionNode expression = new ExpressionNode();
                    expression.Parse(ref tokenQueue);
                    this.childs.Add(expression);
                    step = 2;
                    break;
                    
                case 2: // Expecting closing bracket "]"
                    switch(token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.left_bracket:
                            tokenQueue.Dequeue(); // Consume "]"
                            step = 3;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token
							// TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 3: // Expecting type identifier
                    switch(token) {
                        case Identifier identifier:
                            this.identifier = identifier.getIdentifier(); // Store element type
                            tokenQueue.Dequeue(); // Consume type identifier
                            parsing = false;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token
							// TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
				}
			}
		}
	}

    public class AssignmentNode : IdentifierNode {
    
    public AssignmentNode() : base() {}

    public override void Parse(ref Queue<Token> tokenQueue) {
        int step = 0;
        bool parsing = true;
        
        while(parsing && tokenQueue.Count > 0) {
            Token token = tokenQueue.Peek();
            
            switch(step) {
                case 0: // Expecting identifier (variable name)
                    switch(token) {
                        case Identifier identifier:
                            this.identifier = identifier.getIdentifier();
                            tokenQueue.Dequeue(); // Consume identifier
                            step = 1;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token
							// TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 1: // Expecting assignment operator ":="
                    switch(token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.bare_assignment:
                            tokenQueue.Dequeue(); // Consume ":="
                            step = 2;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token
							// TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 2: // Parse the expression
                    ExpressionNode expression = new ExpressionNode();
                    expression.Parse(ref tokenQueue);
                    this.childs.Add(expression);
                    step = 3;
                    break;
                    
                case 3: // Expecting semicolon
                    switch(token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.end_of_line:
                            tokenQueue.Dequeue(); // Consume ";"
                            parsing = false;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token
							// TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                }
            }
        }
    }

    public abstract class SubprogramNode : Node {
        protected ProgramNode nested;

        public SubprogramNode() : base() {}
    }

    public class ForNode : SubprogramNode {
    private string iterator;
    
    public ForNode() : base() { }

    public override void Parse(ref Queue<Token> tokenQueue) {
        int step = 0;
        bool parsing = true;

        while (parsing && tokenQueue.Count > 0) {
            Token token = tokenQueue.Peek();

            switch (step) {
                case 0: // Expecting "for" keyword
                    switch (token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.for_statement:
                            tokenQueue.Dequeue(); // Consume "for"
                            step = 1;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 1: // Expecting iterator identifier
                    switch (token) {
                        case Identifier identifier:
                            this.iterator = identifier.getIdentifier();
                            tokenQueue.Dequeue(); // Consume identifier
                            step = 2;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 2: // Expecting "in" keyword
                    switch (token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.in_range_statement:
                            tokenQueue.Dequeue(); // Consume "in"
                            step = 3;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 3 or 5: // Parse start expression
                    ExpressionNode RangeExpression = new ExpressionNode();
                    RangeExpression.Parse(ref tokenQueue);
                    this.childs.Add(RangeExpression);
                    step += 1;
                    break;
                    
                case 4: // Expecting ".." range operator
                    switch (token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.range:
                            tokenQueue.Dequeue(); // Consume ".."
                            step = 6;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 6: // Expecting "loop" keyword
                    switch (token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.loop_start:
                            tokenQueue.Dequeue(); // Consume "loop"
                            step = 7;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 7: // Parse loop body
                    // Create and parse the nested program for the loop body
                    this.nested = new ProgramNode();
                    this.nested.Parse(ref tokenQueue);                
                    
                    step = 8;
                    break;
                    
                case 8: // Expecting "end" keyword
                    switch (token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.end_of_body:
                            tokenQueue.Dequeue(); // Consume "end"
                            step = 9;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 9: // Expecting semicolon
                    switch (token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.end_of_line:
                            tokenQueue.Dequeue(); // Consume ";"
                            parsing = false; // Successfully completed
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
            }
        }
    }
    
    // Helper method to get iterator name
    public string GetIterator() => this.iterator;
}

    public class WhileNode : SubprogramNode {
    private ExpressionNode condition;
    
    public WhileNode() : base() { }

    public override void Parse(ref Queue<Token> tokenQueue) {
        int step = 0;
        bool parsing = true;

        while (parsing && tokenQueue.Count > 0) {
            Token token = tokenQueue.Peek();

            switch (step) {
                case 0: // Expecting "while" keyword
                    switch (token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.while_statement:
                            tokenQueue.Dequeue(); // Consume "while"
                            step = 1;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 1: // Parse condition expression
                    this.condition = new ExpressionNode();
                    this.condition.Parse(ref tokenQueue);
                    step = 2;
                    break;
                    
                case 2: // Expecting "loop" keyword
                    switch (token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.loop_start:
                            tokenQueue.Dequeue(); // Consume "loop"
                            step = 3;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 3: // Parse loop body
                    // Create and parse the nested program for the loop body
                    this.nested = new ProgramNode();
                    this.nested.Parse(ref tokenQueue);
                    step = 4;
                    break;
                    
                case 4: // Expecting "end" keyword
                    switch (token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.end_of_body:
                            tokenQueue.Dequeue(); // Consume "end"
                            step = 5;
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
                    
                case 5: // Expecting semicolon
                    switch (token) {
                        case Dedicated dedicated when dedicated.getCode() == DedicatedWord.end_of_line:
                            tokenQueue.Dequeue(); // Consume ";"
                            parsing = false; // Successfully completed
                            break;
                        case Mock:
                            tokenQueue.Dequeue();
                            break;
                        default:
                            // Unexpected token 
                            // TODO: Add exception throw
                            parsing = false;
                            break;
                    }
                    break;
            }
        }
    }
    
    // Helper method to get condition
    public ExpressionNode GetCondition() => this.condition;
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
