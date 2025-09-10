using Lexer.TokenTree;
using Lexer.IO;

/*
Start -- [буква] --> Identifier
Start -- [цифра] --> Number
Start -- [:] --> AssignmentOrColon
Start -- [.] --> DotOrRange
Start -- [<] --> Less
Start -- [>] --> Greater
Start -- [/] --> NotEqual
Start -- ["] --> String
Start -- [#] --> Comment

Identifier -- [буква/цифра] --> Identifier
Identifier -- [другой] --> завершение идентификатора -> Start

Number -- [цифра] --> Number
Number -- [.] --> Decimal
Number -- [не цифра/не точка] --> завершение числа -> Start

Decimal -- [цифра] --> Decimal
Decimal -- [не цифра] --> завершение числа -> Start

AssignmentOrColon -- [=] --> завершение := -> Start
AssignmentOrColon -- [другой] --> завершение : -> Start

DotOrRange -- [.] --> DotOrRange (ждем третью точку для диапазона)
DotOrRange -- [другой] --> завершение . -> Start

Less -- [=] --> завершение <= -> Start
Less -- [другой] --> завершение < -> Start

Greater -- [=] --> завершение >= -> Start
Greater -- [другой] --> завершение > -> Start

NotEqual -- [=] --> завершение /= -> Start
NotEqual -- [другой] --> завершение / -> Start
*/

namespace Lexer.FSM
{
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
			new List<char>() {',', '(', ')', '[', ']', '+', '-', '*', '%'};

		public Lexer(FileReader reader, Queue<Token> stream) {
            this.currentState = new StartState();
            this.currentStateCode = StateCode.Start;
			this.TokenStream = stream;
			this.reader = reader;
		}

        public void ParseFile()
        {
			while(!reader.Empty()) {
				char nextChar = reader.GetNextChar();
				StateCode nextLexerState = StateCode.Start;

				if(separators.Contains(nextChar)) {
					TokenStream.Enqueue(this.currentState.CreateToken());

					Token separatorToken = new Mock();
					switch (nextChar) {
						case ',':
							separatorToken = new Dedicated(DedicatedWord.comma_separator);
							break;

						case '(':
							separatorToken = new Dedicated(DedicatedWord.right_parenthesis);
							break;

						case ')':
							separatorToken = new Dedicated(DedicatedWord.left_parenthesis);
							break;

						case '[':
							separatorToken = new Dedicated(DedicatedWord.right_bracket);
							break;

						case ']':
							separatorToken = new Dedicated(DedicatedWord.left_bracket);
							break;

						case '+':
							separatorToken = new Dedicated(DedicatedWord.summation);
							break;

						case '-':
							separatorToken = new Dedicated(DedicatedWord.difference);
							break;

						case '*':
							separatorToken = new Dedicated(DedicatedWord.multiplication);
							break;

						case '%':
							separatorToken = new Dedicated(DedicatedWord.int_division);
							break;
					}
					TokenStream.Enqueue(separatorToken);

				} else if (char.IsWhiteSpace(nextChar))
					TokenStream.Enqueue(this.currentState.CreateToken());

				else if (nextChar == ':') {
					TokenStream.Enqueue(this.currentState.CreateToken());
					nextLexerState = StateCode.ColonOrAssignment;

				} else if (nextChar == '/') {
					TokenStream.Enqueue(this.currentState.CreateToken());
					nextLexerState = StateCode.DivOrNotEqual;

				} else if (nextChar == '.') {
					TokenStream.Enqueue(this.currentState.CreateToken());
					nextLexerState = StateCode.DotOrRange;

				} else
					nextLexerState = this.currentState.ProccessSymbol(nextChar);

				if (nextLexerState != this.currentStateCode) {
					TokenStream.Enqueue(this.currentState.CreateToken());

					switch(nextLexerState) {
						case StateCode.Start:
							this.currentState = new StartState();
							break;

						case StateCode.Separator:
							this.currentState = new StartState();
							break;

						case StateCode.Identifier:
							this.currentState = new IdentifierState(nextChar);
							break;

						case StateCode.Integer:
							this.currentState = new IntegerState(nextChar);
							break;

						case StateCode.Less:
							this.currentState = new LessState();
							break;

						case StateCode.Greater:
							this.currentState = new GreaterState();
							break;

						case StateCode.EqualOrOneliner:
							this.currentState = new EqualState();
							break;

						case StateCode.DotOrRange:
							this.currentState = new DotOrRangeState();
							break;

						case StateCode.ColonOrAssignment:
							this.currentState = new ColonOrAssignmentState();
							break;

						case StateCode.DivOrNotEqual:
							this.currentState = new DivOrNotEqualState();
							break;

						case StateCode.Error:
							this.currentState = new StartState();
							throw new Exception("Unrecognizable Token");
					}
				}
			}
        }
    }

    public abstract class State {
        public abstract StateCode ProccessSymbol(char symbol);
		public abstract Token CreateToken();
    }

    public class StartState : State {
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
		protected StoringState() => this.data = "";
	}
	
	public class IdentifierState : StoringState {
		public IdentifierState(char symbol) : base() => this.data += symbol;

		protected Dictionary<string,DedicatedWord> dedicatedWords =
			new Dictionary<string,DedicatedWord>() {
				{"var",		DedicatedWord.variable_declaration},
				{"is",		DedicatedWord.is_assignment},
				{"type",	DedicatedWord.type_assignment},
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
				{"routnine",DedicatedWord.routine_declaration},
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
				return new Dedicated(dedicatedWords[data]);
			return new Identifier(data);
		}
	}

	public class IntegerState : StoringState {
		public IntegerState(char symbol) : base() => this.data += symbol;

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
			return new Integer(int.Parse(data));
		}
	}

	public abstract class ChoosingState : State {
		protected bool single;
		protected ChoosingState() => this.single = true;
	}

	public class LessState : ChoosingState {
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
				return new Dedicated(DedicatedWord.less);
			return new Dedicated(DedicatedWord.less_equal);
		}
	}

	public class GreaterState : ChoosingState {
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
				return new Dedicated(DedicatedWord.greater);
			return new Dedicated(DedicatedWord.greater_equal);
		}
	}

	public class EqualState : ChoosingState {
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
				return new Dedicated(DedicatedWord.equal);
			return new Dedicated(DedicatedWord.one_line_body);
		}
	}

	public class DivOrNotEqualState : ChoosingState {
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
				return new Dedicated(DedicatedWord.division);
			return new Dedicated(DedicatedWord.not_equal);
		}
	}

	public class ColonOrAssignmentState : ChoosingState {
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
				return new Dedicated(DedicatedWord.type_assignment);
			return new Dedicated(DedicatedWord.bare_assignment);
		}
	}

	public class DotOrRangeState : ChoosingState {
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
				return new Dedicated(DedicatedWord.dot);
			return new Dedicated(DedicatedWord.range);
		}
	}
}
