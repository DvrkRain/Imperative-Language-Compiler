namespace Lexer.TokenTree {
	public enum DedicatedWord {
		// Variable declaration
		variable_declaration,
		type_declaration,
		// Built-in types
		integer_type,
		real_type,
		boolean_type,
		record_type,
		array_type,
		// Assignment
		type_assignment,
		declaration_assignment,
		bare_assignment,
		// Separators
		field_access,
		comma_separator,
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
		reverse_order_statement,
		// Routines
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
		int_division
	}

	public abstract class Token { }

	public class Identifier : Token {
		public string identifier;

		public Identifier(string identifier) : base() {
			this.identifier = identifier;
		}
	}

	public class Dedicated : Token {
		public DedicatedWord code;

		public Dedicated(DedicatedWord code) : base() {
			this.code = code;
		}
	}

	public abstract class Literal : Token { }

	public class Integer : Literal {
		public int value;

		public Integer(int val) : base() {
			this.value = val;
		}
	}

	public class Real : Literal {
		public float value;

		public Real(float val) : base() {
			this.value = val;
		}
	}
}
