using LexicalAnalyzer.TokenTree;

namespace SyntaxAnalyzer {
    public abstract class Node {
        protected List<Node> childs;

        public Node() => this.childs = new List<Node>();

        public abstract void Parse(ref Queue<Token> tokenQueue);

        public List<Node> GetChilds() {
            return this.childs;
        }
    }

    public class ProgramNode : Node {
        public bool main;

        public ProgramNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {
			while(tokenQueue.Count > 0) {
				Token token = tokenQueue.Dequeue();
				switch(token) {
					case Dedicated dedicated when dedicated.getCode() == DedicatedWord.variable_declaration:
						Node var_decl = new VarNode();
						var_decl.Parse(ref tokenQueue);
						this.childs.Add(var_decl);
						break;
						
					case Dedicated dedicated when dedicated.getCode() == DedicatedWord.type_declaration:
						Node type_decl = new VarNode();
						type_decl.Parse(ref tokenQueue);
						this.childs.Add(type_decl);
						break;
						
					case Dedicated dedicated when dedicated.getCode() == DedicatedWord.routine_declaration:
						Node rout_decl = new VarNode();
						rout_decl.Parse(ref tokenQueue);
						this.childs.Add(rout_decl);
						break;
						
					case Dedicated dedicated when dedicated.getCode() == DedicatedWord.if_statement:
						Node if_stnt = new VarNode();
						if_stnt.Parse(ref tokenQueue);
						this.childs.Add(if_stnt);
						break;
						
					case Dedicated dedicated when dedicated.getCode() == DedicatedWord.for_statement:
						Node for_stnt = new VarNode();
						for_stnt.Parse(ref tokenQueue);
						this.childs.Add(for_stnt);
						break;
						
					case Dedicated dedicated when dedicated.getCode() == DedicatedWord.while_statement:
						Node while_stnt = new VarNode();
						while_stnt.Parse(ref tokenQueue);
						this.childs.Add(while_stnt);
						break;
						
					default:
						break;
				}
			}
		}
    }

    public class IfNode : Node {
        public List<BranchNode> branches;

        public IfNode() : base() => this.branches = new List<BranchNode>();

        public override void Parse(ref Queue<Token> tokenQueue) {
            int step = 0;
            bool parsing = true;
            
            while(parsing && tokenQueue.Count > 0) {
                Token token = tokenQueue.Peek();
                
                // Assume that "if" keyword is already consumed
                switch(step) {
                    case 0: // Parse condition expression
                        ExpressionNode condition = new ExpressionNode();
                        condition.Parse(ref tokenQueue);
                        this.childs.Add(condition);

                        step = 1;
                        break;
                        
                    case 1: // Expecting "then" keyword
                        switch(token) {
                            case Dedicated dedicated when dedicated.getCode() == DedicatedWord.then_branch:
                                tokenQueue.Dequeue(); // Consume "then"
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
                        
                    case 2 or 4: // Parse branch statements until "else" or "end"
                        BranchNode newBranch = new BranchNode();
                        newBranch.Parse(ref tokenQueue);
                        this.childs.Add(newBranch);

                        step += 1;
                        break;
                        
                    case 3: // Check for "else" or "end"
                        switch(token) {
                            case Dedicated dedicated when dedicated.getCode() == DedicatedWord.else_branch:
                                tokenQueue.Dequeue(); // Consume "else"
                                step = 4;
                                break;
                                
                            case Dedicated dedicated when dedicated.getCode() == DedicatedWord.end_of_body:
                                tokenQueue.Dequeue(); // Consume "end"
                                step = 5; // Proceed to final validation
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

        public VarNode() : base() => this.type = "void";

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
		public static List<DedicatedWord> allowedOperations = new List<DedicatedWord>() {
			DedicatedWord.logical_and,
			DedicatedWord.logical_or,
			DedicatedWord.logical_xor,
			DedicatedWord.logical_not,
			DedicatedWord.less_equal,
			DedicatedWord.less,
			DedicatedWord.equal,
			DedicatedWord.greater_equal,
			DedicatedWord.greater,
			DedicatedWord.not_equal,
			DedicatedWord.summation,
			DedicatedWord.difference,
			DedicatedWord.multiplication,
			DedicatedWord.division,
			DedicatedWord.int_division,
		};

		public static List<DedicatedWord> unaryOperations = new List<DedicatedWord>() {
			DedicatedWord.logical_not,
			DedicatedWord.summation,
			DedicatedWord.difference,
		};

		public static List<DedicatedWord> logicalOperations = new List<DedicatedWord>() {
			DedicatedWord.logical_and,
			DedicatedWord.logical_or,
			DedicatedWord.logical_xor,
			DedicatedWord.logical_not,
		};

		public static List<DedicatedWord> relationOperations = new List<DedicatedWord>() {
			DedicatedWord.less_equal,
			DedicatedWord.less,
			DedicatedWord.equal,
			DedicatedWord.greater_equal,
			DedicatedWord.greater,
			DedicatedWord.not_equal,
		};

		public static List<DedicatedWord> factorOperations = new List<DedicatedWord>() {
			DedicatedWord.multiplication,
			DedicatedWord.division,
			DedicatedWord.int_division,
		};

		public static List<DedicatedWord> termOperations = new List<DedicatedWord>() {
			DedicatedWord.summation,
			DedicatedWord.difference,
		};

		public DedicatedWord opCode;
		public ExpressionNode left;
		public ExpressionNode right;
		public bool initialized;

		public ExpressionNode() : base() {
			this.initialized = false;
		}
		public ExpressionNode(ExpressionNode init, DedicatedWord operation) : base() {
			this.initialized = true;
			this.left = init;
			this.opCode = operation;
		}

		public override void Parse(ref Queue<Token> tokenQueue) {
			int step = 0;
			if(initialized) {
				step = 2;
			}
			bool parenthesised = false;
			while(step < 0) {
				Token token = tokenQueue.Peek();
				switch(step) {
					case 0:
						switch(token) {
							case Dedicated dedicated when dedicated.getCode() == DedicatedWord.left_parenthesis:
								if(parenthesised) {
									this.left = new ExpressionNode();
									this.left.Parse(ref tokenQueue);
									step = 1;
								} else {
									parenthesised = true;
									tokenQueue.Dequeue();
								}
								break;

							case Identifier var:
								this.left = new PrimaryNode(var.getIdentifier());
								step = 1;
								tokenQueue.Dequeue();
								break;

							case Integer lit:
								this.left = new PrimaryNode(lit.getValue());
								step = 1;
								tokenQueue.Dequeue();
								break;

							case Dedicated dedicated when dedicated.getCode() == DedicatedWord.false_const:
								this.left = new PrimaryNode(false);
								step = 1;
								tokenQueue.Dequeue();
								break;

							case Dedicated dedicated when dedicated.getCode() == DedicatedWord.true_const:
								this.left = new PrimaryNode(true);
								step = 1;
								tokenQueue.Dequeue();
								break;

							case Dedicated dedicated when unaryOperations.Contains(dedicated.getCode()):
								this.left = new PrimaryNode(0);
								this.opCode = dedicated.getCode();
								step = 2;
								tokenQueue.Dequeue();
								break;

							default:
								// Unexpected token
								break;
						}
						break;

					case 1:
						switch(token) {
							case Dedicated dedicated when dedicated.getCode() == DedicatedWord.dot:
								tokenQueue.Dequeue();
								this.left = new ExpressionNode(this.left, DedicatedWord.dot);
								this.left.Parse(ref tokenQueue);
								break;

							case Dedicated dedicated when allowedOperations.Contains(dedicated.getCode()):
								this.opCode = dedicated.getCode();
								step = 2;
								tokenQueue.Dequeue();
								break;

							default:
								break;
						}
						break;

					case 2:
						switch(token) {
							case Dedicated dedicated when dedicated.getCode() == DedicatedWord.logical_not:
								tokenQueue.Dequeue();
								this.right = new ExpressionNode(new PrimaryNode(0), DedicatedWord.logical_not);
								this.right.Parse(ref tokenQueue);
								step = 3;
								break;

							case Dedicated dedicated when dedicated.getCode() == DedicatedWord.left_parenthesis:
								this.left = new ExpressionNode();
								this.left.Parse(ref tokenQueue);
								step = 3;
								break;

							case Identifier var:
								this.left = new PrimaryNode(var.getIdentifier());
								step = 3;
								tokenQueue.Dequeue();
								break;

							case Integer lit:
								this.left = new PrimaryNode(lit.getValue());
								step = 3;
								tokenQueue.Dequeue();
								break;

							case Dedicated dedicated when dedicated.getCode() == DedicatedWord.false_const:
								this.left = new PrimaryNode(false);
								step = 3;
								tokenQueue.Dequeue();
								break;

							case Dedicated dedicated when dedicated.getCode() == DedicatedWord.true_const:
								this.left = new PrimaryNode(true);
								step = 3;
								tokenQueue.Dequeue();
								break;

							default:
								break;
						}
						break;

					case 3:
						break;

					default:
						break;
				}
			}
		}
	}

	public class PrimaryNode : ExpressionNode {
		public object value;

		public PrimaryNode(object val) : base() {
			this.value = val;
		}
	}
}
