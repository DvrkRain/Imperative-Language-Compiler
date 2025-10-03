namespace LexicalAnalyzer.TokenTree {
	public enum DedicatedWord {
		// Variable declaration
		variable_declaration,
		type_declaration,
		// Built-in types
		integer_type,
		real_type,
		boolean_type,
		true_const,
		false_const,
		record_type,
		array_type,
		// Assignment
		type_assignment,
		bare_assignment,
		is_assignment,
		// Separators
		dot,
		comma_separator,
		end_of_line,
		// Delimeters
		left_parenthesis,
		right_parenthesis,
		left_bracket,
		right_bracket,
		// Branching
		if_statement,
		then_branch,
		else_branch,
		// Loops
		loop_start,
		while_statement,
		for_statement,
		in_range_statement,
		range,
		reverse_order_statement,
		// Routines
		print_routine,
		routine_declaration,
		one_line_body,
		end_of_body,
		// Logic
		logical_and,
		logical_or,
		logical_xor,
		logical_not,
		// Relation
		less,
		less_equal,
		equal,
		greater_equal,
		greater,
		not_equal,
		// Math
		summation,
		difference,
		multiplication,
		division,
		int_division,
	}

	public struct Position {
		public Position(int x, int y) {
			this.row = x;
			this.col = y;
		}
		public int row {get; set;}
		public int col {get; set;}
	}

	public abstract class Token {
		protected Position pos{get;}
		protected Token(int x, int y) =>
			pos = new Position(x,y);
		protected Token(Position pos) =>
			this.pos = pos;

		public abstract void PrintInfo();
	}

	public class Mock : Token {
		public Mock() : base(0,0) {}
		public override void PrintInfo() =>
			Console.WriteLine("Mock token");
	}

	public class Identifier : Token {
		protected string identifier{get;}

		public Identifier(Position pos, string identifier) : base(pos) {
			this.identifier = identifier;
		}

		public string getIdentifier() {
			return this.identifier;
		}

		public override void PrintInfo() =>
			Console.WriteLine($"Identifier token at {this.pos.row},{this.pos.col}. Identifier is {this.identifier}.");
	}

	public class Dedicated : Token {
		protected DedicatedWord code{get;}

		public Dedicated(Position pos, DedicatedWord code) : base(pos) {
			this.code = code;
		}

		public DedicatedWord getCode() {
			return this.code;
		}

		public override void PrintInfo() =>
			Console.WriteLine($"Dedicated word token at {this.pos.row},{this.pos.col}. Keyword is {this.code}.");
	}

	public class Integer : Token {
		protected int value{get;}

		public Integer(Position pos, int val) : base(pos) {
			this.value = val;
		}

		public override void PrintInfo() =>
			Console.WriteLine($"Number token at {this.pos.row},{this.pos.col}. Number is {this.value}.");
	}
}
