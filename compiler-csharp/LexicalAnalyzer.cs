using Data.ErrorHandling;
using Data.Objects;
using Data.IO;


namespace LexicalAnalyzer
{
    // Enum is needed to easy check state switching
    public enum StateCode
    {
        Start,
        Identifier,
        Numeric,
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

		public Lexer() {
            this.currentState = new StartState();
            this.currentStateCode = StateCode.Start;
		}

        public void ParseFile(ref Queue<Token> TokenStream)
        {
			Position cursor = new Position(1,1);
			while(!FileReader.Empty()) {
				char nextChar = FileReader.Get();
                StateCode nextStateCode = StateCode.Start;

				if(SeparatorList.Contains(nextChar)) {
					this.currentState.AddToken(ref TokenStream);
                    Token separatorToken = new Token(cursor, SeparatorList.Code(nextChar), char.ToString(nextChar));
					TokenStream.Enqueue(separatorToken);

				} else if (nextChar == '\n') {
					nextStateCode = StateCode.Start;
					cursor.NextLine();

				} else if (char.IsWhiteSpace(nextChar)) {
					nextStateCode = StateCode.Start;

				} else if (nextChar == ':') {
					if (this.currentStateCode == StateCode.ColonOrAssignment)
						this.currentState.AddToken(ref TokenStream);
					nextStateCode = StateCode.ColonOrAssignment;

				} else if (nextChar == '/') {
					if (this.currentStateCode == StateCode.DivOrNotEqual)
						this.currentState.AddToken(ref TokenStream);
					nextStateCode = StateCode.DivOrNotEqual;

				} else if (nextChar == '.'
						&& this.currentStateCode == StateCode.Numeric
							&& !((NumeralState)this.currentState).real) {
					nextStateCode = this.currentState.ProccessSymbol(nextChar);

				} else if (nextChar == '.'
						&& this.currentStateCode != StateCode.DotOrRange) {
					nextStateCode = StateCode.DotOrRange;

				} else
					nextStateCode = this.currentState.ProccessSymbol(nextChar);

                try {
                    if (nextStateCode != this.currentStateCode) {
                        if (!SeparatorList.Contains(nextChar))
                            this.currentState.AddToken(ref TokenStream);

                        this.currentStateCode = nextStateCode;

                        switch (nextStateCode) {
                            case StateCode.Start:
                                this.currentState = new StartState();
                                break;

                            case StateCode.Identifier:
                                this.currentState = new IdentifierState(cursor, nextChar);
                                break;

                            case StateCode.Numeric:
                                this.currentState = new NumeralState(cursor, nextChar);
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

                                ErrorHandling.UnknownSymbol(cursor);
                                break;
                        }
                    }
                } catch (Exception e) {
                    Console.WriteLine(e.Message);
                    System.Environment.Exit(1);
                }

                cursor.NextChar();
			}
			TokenStream.Enqueue(new Token(cursor, TokenCode.end_of_file));
        }
    }

    public abstract class State {
		protected Position position{get;}
		public State(Position pos) =>
			this.position = pos;

        public abstract StateCode ProccessSymbol(char symbol);
		public abstract void AddToken(ref Queue<Token> tokenQueue);
    }

    public class StartState : State {
		public StartState() : base(new Position(0,0)) { }

        public override StateCode ProccessSymbol(char symbol) {
			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (char.IsDigit(symbol)) return StateCode.Numeric;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;
			if (symbol == '=') return StateCode.EqualOrOneliner;

            return StateCode.Error;
        }

		public override void AddToken(ref Queue<Token> tokenQueue) {}
    }

	public abstract class StoringState : State {
		protected string data;
		protected StoringState(Position pos) : base(pos) => this.data = "";
	}
	
	public class IdentifierState : StoringState {
		public IdentifierState(Position pos, char symbol) : base(pos) => this.data += symbol;

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

		public override void AddToken(ref Queue<Token> tokenQueue) {
			Token token;
			if(this.data == "true") 
				token = new Token(this.position, DedicatedWords.Code(this.data), true);
			else if(this.data == "false") 
				token = new Token(this.position, DedicatedWords.Code(this.data), false);
			else if(DedicatedWords.Keys(this.data))
				token = new Token(this.position, DedicatedWords.Code(this.data), this.data);
			else
				token = new Token(this.position, TokenCode.identifier, this.data);

			tokenQueue.Enqueue(token);
		}
	}

	public class NumeralState : StoringState {
		public bool real = false;
		public NumeralState(Position pos, char symbol) : base(pos) => this.data += symbol;

		public override StateCode ProccessSymbol(char symbol) {
			if (char.IsDigit(symbol)) {
				this.data += symbol;
				return StateCode.Numeric;
			}

			if (symbol == '.') {
				if(this.real)
					return StateCode.DotOrRange;
				else {
					this.data += ',';
					this.real = true;
					return StateCode.Numeric;
				}
			}

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;
			if (symbol == '=') return StateCode.EqualOrOneliner;

			return StateCode.Error;
		}

		public override void AddToken(ref Queue<Token> tokenQueue) {
			if(this.real && float.TryParse(this.data, out float f)) 
				tokenQueue.Enqueue(new Token(this.position, TokenCode.constant_value, f));
			else if(int.TryParse(this.data, out int i))
				tokenQueue.Enqueue(new Token(this.position, TokenCode.constant_value, i));
			else
				ErrorHandling.UnknownToken(this.position);
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
			if (char.IsDigit(symbol)) return StateCode.Numeric;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Error;
			if (symbol == '>') return StateCode.Greater;

			return StateCode.Error;
		}

		public override void AddToken(ref Queue<Token> tokenQueue) {
			Token token = new Token(this.position, TokenCode.relation_op);
			if (single)
				token.Value("<");
			else
				token.Value("<=");
			tokenQueue.Enqueue(token);
		}
	}

	public class GreaterState : ChoosingState {
		public GreaterState(Position pos) : base(pos) { }
		
		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '=') {
				this.single = false;
				return StateCode.Start;
			}
			if (char.IsDigit(symbol)) return StateCode.Numeric;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;

			return StateCode.Error;
		}

		public override void AddToken(ref Queue<Token> tokenQueue) {
			Token token = new Token(this.position, TokenCode.relation_op);
			if (single)
				token.Value(">");
			else
				token.Value(">=");
			tokenQueue.Enqueue(token);
		}
	}

	public class EqualState : ChoosingState {
		bool state;
		public EqualState(Position pos) : base(pos) => this.state = false;
		
		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '>') {
				this.single = false;
				return StateCode.Start;
			}
			if (symbol == '=') {
				this.single = false;
				this.state = true;
				return StateCode.Start;
			}
			if (char.IsDigit(symbol)) return StateCode.Numeric;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;

			return StateCode.Error;
		}

		public override void AddToken(ref Queue<Token> tokenQueue) {
			Token token;
			if (single)
                ErrorHandling.UnknownToken(this.position);
			else {
				if(state)
					token = new Token(this.position, TokenCode.relation_op, "==");
				else
					token = new Token(this.position, TokenCode.one_line_body);
				tokenQueue.Enqueue(token);
			}
		}
	}

	public class DivOrNotEqualState : ChoosingState {
		public DivOrNotEqualState(Position pos) : base(pos) { }

		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '=') {
				this.single = false;
				return StateCode.Start;
			}
			if (char.IsDigit(symbol)) return StateCode.Numeric;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;

			return StateCode.Error;
		}

		public override void AddToken(ref Queue<Token> tokenQueue) {
			Token token;
			if (single)
				token = new Token(this.position, TokenCode.factor_op, "/");
			else
				token = new Token(this.position, TokenCode.relation_op, "/=");
			tokenQueue.Enqueue(token);
		}
	}

	public class ColonOrAssignmentState : ChoosingState {
		public ColonOrAssignmentState(Position pos) : base(pos) { }

		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '=') {
				this.single = false;
				return StateCode.Start;
			}
			if (char.IsDigit(symbol)) return StateCode.Numeric;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;

			return StateCode.Error;
		}

		public override void AddToken(ref Queue<Token> tokenQueue) {
			Token token;
			if (single)
				token = new Token(this.position, TokenCode.type_assignment);
			else
				token = new Token(this.position, TokenCode.bare_assignment);
			tokenQueue.Enqueue(token);
		}
	}

	public class DotOrRangeState : ChoosingState {
		public DotOrRangeState(Position pos) : base(pos) { }

		public override StateCode ProccessSymbol(char symbol) {
			if (symbol == '.') {
				this.single = false;
				return StateCode.Start;
			}
			if (char.IsDigit(symbol)) return StateCode.Numeric;

			if (char.IsLetter(symbol)) return StateCode.Identifier;

			if (symbol == '=') return StateCode.EqualOrOneliner;
			if (symbol == '<') return StateCode.Less;
			if (symbol == '>') return StateCode.Greater;

			return StateCode.Error;
		}

		public override void AddToken(ref Queue<Token> tokenQueue) {
			Token token;
			if (single)
				token = new Token(this.position, TokenCode.dot, ".");
			else
				token = new Token(this.position, TokenCode.range_sign, "..");
			tokenQueue.Enqueue(token);
		}
	}
}
