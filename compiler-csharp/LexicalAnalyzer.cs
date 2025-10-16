using LexicalAnalyzer.TokenTree;
using LexicalAnalyzer.IO;


namespace LexicalAnalyzer
{
	public class UnexpectedTokenException : Exception {
		public Position pos;
		public UnexpectedTokenException(Position pos) : base("Unexpected token") => this.pos = pos;
		public void Info() =>
			Console.WriteLine($"Unrecognizable token at {this.pos.row},{this.pos.col}.");
	}

    // Enum is needed to easy check state switching
    public enum StateCode
    {
        Start,
		Separator,
        Identifier,
        Integer,
        Less,
        Greater,
		EqualOrOneliner,
        ColonOrAssignment,
        DotOrRange,
        DivOrNotEqual,
        Error
    }

    public class Lexer
    {
        private State currentState;
        private StateCode currentStateCode;
		public Queue<Token> TokenStream {get; set;}
		public FileReader reader;

		private static List<char> separators =
			new List<char>() {',', '(', ')', '[', ']', '+', '-', '*', '%', ';'};

		public Lexer(FileReader reader, Queue<Token> stream) {
            this.currentState = new StartState();
            this.currentStateCode = StateCode.Start;
			this.TokenStream = stream;
			this.reader = reader;
		}

        public void ParseFile()
        {
			Position cursor = new Position(1,1);
			while(!reader.Empty()) {
				char nextChar = reader.GetNextChar();
				StateCode nextStateCode = StateCode.Start;

				if(separators.Contains(nextChar)) {
					Token token = this.currentState.CreateToken();
					if(token is not Mock)
						TokenStream.Enqueue(token);
					Token separatorToken = new Mock();
					switch (nextChar) {
						case ',':
							separatorToken = new Dedicated(cursor, DedicatedWord.comma_separator);
							break;

						case '(':
							separatorToken = new Dedicated(cursor, DedicatedWord.left_parenthesis);
							break;

						case ')':
							separatorToken = new Dedicated(cursor, DedicatedWord.right_parenthesis);
							break;

						case '[':
							separatorToken = new Dedicated(cursor, DedicatedWord.left_bracket);
							break;

						case ']':
							separatorToken = new Dedicated(cursor, DedicatedWord.right_bracket);
							break;

						case '+':
							separatorToken = new Dedicated(cursor, DedicatedWord.summation);
							break;

						case '-':
							separatorToken = new Dedicated(cursor, DedicatedWord.difference);
							break;

						case '*':
							separatorToken = new Dedicated(cursor, DedicatedWord.multiplication);
							break;

						case '%':
							separatorToken = new Dedicated(cursor, DedicatedWord.int_division);
							break;

						case ';':
							separatorToken = new Dedicated(cursor, DedicatedWord.end_of_line);
							break;
					}
					TokenStream.Enqueue(separatorToken);

				} else if (nextChar == '\n') {
					nextStateCode = StateCode.Start;
					cursor.row += 1;
					cursor.col = 0;

				} else if (char.IsWhiteSpace(nextChar))
					nextStateCode = StateCode.Start;

				else if (nextChar == ':') {
					if (this.currentStateCode == StateCode.ColonOrAssignment)
						TokenStream.Enqueue(this.currentState.CreateToken());
					nextStateCode = StateCode.ColonOrAssignment;

				} else if (nextChar == '/') {
					if (this.currentStateCode == StateCode.DivOrNotEqual)
						TokenStream.Enqueue(this.currentState.CreateToken());
					nextStateCode = StateCode.DivOrNotEqual;

				} else if (nextChar == '.' && this.currentStateCode != StateCode.DotOrRange) {
					nextStateCode = StateCode.DotOrRange;

				} else
					nextStateCode = this.currentState.ProccessSymbol(nextChar);

				try {
					if (nextStateCode != this.currentStateCode) {
						if(!separators.Contains(nextChar)) {
							Token token = this.currentState.CreateToken();
							if(token is not Mock)
								TokenStream.Enqueue(token);
						}

						this.currentStateCode = nextStateCode;

						switch(nextStateCode) {
							case StateCode.Start:
								this.currentState = new StartState();
								break;

							case StateCode.Separator:
								this.currentState = new StartState();
								this.currentStateCode = StateCode.Start;
								break;

							case StateCode.Identifier:
								this.currentState = new IdentifierState(cursor, nextChar);
								break;

							case StateCode.Integer:
								this.currentState = new IntegerState(cursor, nextChar);
								break;

							case StateCode.Less:
								this.currentState = new LessState(cursor);
								break;

							case StateCode.Greater:
								this.currentState = new GreaterState(cursor);
								break;

							case StateCode.EqualOrOneliner:
								this.currentState = new EqualState(cursor);
								break;

							case StateCode.DotOrRange:
								this.currentState = new DotOrRangeState(cursor);
								break;

							case StateCode.ColonOrAssignment:
								this.currentState = new ColonOrAssignmentState(cursor);
								break;

							case StateCode.DivOrNotEqual:
								this.currentState = new DivOrNotEqualState(cursor);
								break;

							case StateCode.Error:
								this.currentState = new StartState();
								this.currentStateCode = StateCode.Start;
								throw new UnexpectedTokenException(cursor);
						}
					}
				} catch (UnexpectedTokenException e) {
					e.Info();
				} catch (Exception) {
					Console.WriteLine("Unexpected Exception. Terminating...");
					System.Environment.Exit(1);
				}
				cursor.col += 1;
			}
        }
    }

    public abstract class State {
		protected Position pos{get;}
		public State(Position pos) =>
			this.pos = pos;

        public abstract StateCode ProccessSymbol(char symbol);
		public abstract Token CreateToken();
    }

    public class StartState : State {
		public StartState() : base(new Position(0,0)) { }

        public override StateCode ProccessSymbol(char symbol) {
			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (char.IsDigit(symbol)) return StateCode.Integer;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;
			if (symbol == '=') return StateCode.EqualOrOneliner;

            return StateCode.Error;
        }

		public override Token CreateToken() {
			return new Mock();
		}
    }

	public abstract class StoringState : State {
		protected string data;
		protected StoringState(Position pos) : base(pos) => this.data = "";
	}
	
	public class IdentifierState : StoringState {
		public IdentifierState(Position pos, char symbol) : base(pos) => this.data += symbol;

		protected Dictionary<string,DedicatedWord> dedicatedWords =
			new Dictionary<string,DedicatedWord>() {
				{"var",		DedicatedWord.variable_declaration},
				{"is",		DedicatedWord.is_assignment},
				{"type",	DedicatedWord.type_declaration},
				{"integer", DedicatedWord.integer_type},
				{"real",	DedicatedWord.real_type},
				{"boolean", DedicatedWord.boolean_type},
				{"true",	DedicatedWord.true_const},
				{"false",	DedicatedWord.false_const},
				{"record",	DedicatedWord.record_type},
				{"array",	DedicatedWord.array_type},
				{"end",		DedicatedWord.end_of_body},
				{"if",		DedicatedWord.if_statement},
				{"then",	DedicatedWord.then_branch},
				{"else",	DedicatedWord.else_branch},
				{"loop",	DedicatedWord.loop_start},
				{"while",	DedicatedWord.while_statement},
				{"for",		DedicatedWord.for_statement},
				{"in",		DedicatedWord.in_range_statement},
				{"reverse",	DedicatedWord.reverse_order_statement},
				{"print",	DedicatedWord.print_routine},
				{"routine",DedicatedWord.routine_declaration},
				{"and",		DedicatedWord.logical_and},
				{"or",		DedicatedWord.logical_or},
				{"xor",		DedicatedWord.logical_xor},
				{"not",		DedicatedWord.logical_not},
			};

        public override StateCode ProccessSymbol(char symbol) {
			if(char.IsLetter(symbol) || char.IsDigit(symbol) || symbol == '_') {
				this.data += symbol;
				return StateCode.Identifier;
			}

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;
			if (symbol == '=') return StateCode.EqualOrOneliner;

			return StateCode.Error;
		}

		public override Token CreateToken() {
			if (dedicatedWords.Keys.Contains(data))
				return new Dedicated(this.pos, dedicatedWords[data]);
			return new Identifier(this.pos, data);
		}
	}

	public class IntegerState : StoringState {
		public IntegerState(Position pos, char symbol) : base(pos) => this.data += symbol;

		public override StateCode ProccessSymbol(char symbol) {
			if (char.IsDigit(symbol)) {
				this.data += symbol;
				return StateCode.Integer;
			}

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;
			if (symbol == '=') return StateCode.EqualOrOneliner;

			return StateCode.Error;
		}

		public override Token CreateToken() {
			return new Integer(this.pos, int.Parse(data));
		}
	}

	public abstract class ChoosingState : State {
		protected bool single;
		protected ChoosingState(Position pos) : base(pos) => this.single = true;
	}

	public class LessState : ChoosingState {
		public LessState(Position pos) : base(pos) { }

		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '=') {
				this.single = false;
				return StateCode.Start;
			}
			if (char.IsDigit(symbol)) return StateCode.Integer;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;

			return StateCode.Error;
		}

		public override Token CreateToken() {
			if (single)
				return new Dedicated(this.pos, DedicatedWord.less);
			return new Dedicated(this.pos, DedicatedWord.less_equal);
		}
	}

	public class GreaterState : ChoosingState {
		public GreaterState(Position pos) : base(pos) { }
		
		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '=') {
				this.single = false;
				return StateCode.Start;
			}
			if (char.IsDigit(symbol)) return StateCode.Integer;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;

			return StateCode.Error;
		}

		public override Token CreateToken() {
			if (single)
				return new Dedicated(this.pos, DedicatedWord.greater);
			return new Dedicated(this.pos, DedicatedWord.greater_equal);
		}
	}

	public class EqualState : ChoosingState {
		public EqualState(Position pos) : base(pos) { }
		
		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '>') {
				this.single = false;
				return StateCode.Start;
			}
			if (symbol == '=') return StateCode.Start;
			if (char.IsDigit(symbol)) return StateCode.Integer;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;

			return StateCode.Error;
		}

		public override Token CreateToken() {
			if (single)
				return new Dedicated(this.pos, DedicatedWord.equal);
			return new Dedicated(this.pos, DedicatedWord.one_line_body);
		}
	}

	public class DivOrNotEqualState : ChoosingState {
		public DivOrNotEqualState(Position pos) : base(pos) { }

		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '=') {
				this.single = false;
				return StateCode.Start;
			}
			if (char.IsDigit(symbol)) return StateCode.Integer;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;

			return StateCode.Error;
		}

		public override Token CreateToken() {
			if (single)
				return new Dedicated(this.pos, DedicatedWord.division);
			return new Dedicated(this.pos, DedicatedWord.not_equal);
		}
	}

	public class ColonOrAssignmentState : ChoosingState {
		public ColonOrAssignmentState(Position pos) : base(pos) { }

		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '=') {
				this.single = false;
				return StateCode.Start;
			}
			if (char.IsDigit(symbol)) return StateCode.Integer;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;

			return StateCode.Error;
		}

		public override Token CreateToken() {
			if (single)
				return new Dedicated(this.pos, DedicatedWord.type_assignment);
			return new Dedicated(this.pos, DedicatedWord.bare_assignment);
		}
	}

	public class DotOrRangeState : ChoosingState {
		public DotOrRangeState(Position pos) : base(pos) { }

		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '.') {
				this.single = false;
				return StateCode.Start;
			}
			if (char.IsDigit(symbol)) return StateCode.Integer;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '=') return StateCode.EqualOrOneliner;
			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;

			return StateCode.Error;
		}

		public override Token CreateToken() {
			if (single)
				return new Dedicated(this.pos, DedicatedWord.dot);
			return new Dedicated(this.pos, DedicatedWord.range);
		}
	}
}
