namespace Data.Objects {
	public struct Position {
		public Position(int x, int y) {
			this.row = x;
			this.col = y;
		}
		
		private int row {get; set;}
		private int col {get; set;}

		public int Row() => row;
		public int Col() => col;

		public void NextLine() {
			this.row += 1;
			this.col = 0;
		}
		public void NextChar() {
			this.col += 1;
		}
	}

	public enum TokenCode {
		builtin_type,
		constant_value,
		logic_op,
		relation_op,
		factor_op,
		term_op,
		identifier,

		// User-defined types
		record_declaration,
		array_declaration,
		// Variable declaration
		variable_declaration,
		type_declaration,
		// Assingnment
		type_assignment,
		bare_assignment,
		is_assignment,
		// Separators
		dot,
		comma,
		semicolon,
		// Delimeters
		left_parenthesis,
		right_parenthesis,
		left_bracket,
		right_bracket,
		// Branching
		if_statement,
		then_statement,
		else_statement,
		// Loops
		while_statement,
		for_statement,
		in_statement,
		reverse_statement,
		loop_start,
		range_sign,
		// Routines
		print_routine,
		routine_declaration,
		one_line_body,
		end_of_body,
		// Sequence breaking
		return_statement,
		break_statement,
		continue_statement,
		end_of_file,
	}

	public static class DedicatedWords {
		private static Dictionary<string,TokenCode> dedicatedWords =
			new Dictionary<string,TokenCode>() {
				{"integer",		TokenCode.builtin_type},
				{"real",		TokenCode.builtin_type},
				{"boolean", 	TokenCode.builtin_type},
				{"record",		TokenCode.record_declaration},
				{"array",		TokenCode.array_declaration},
				{"and",			TokenCode.logic_op},
				{"or",			TokenCode.logic_op},
				{"xor",			TokenCode.logic_op},
				{"not",			TokenCode.logic_op},
				{"true",		TokenCode.constant_value},
				{"false",		TokenCode.constant_value},
				{"var",			TokenCode.variable_declaration},
				{"is",			TokenCode.is_assignment},
				{"type",		TokenCode.type_declaration},
				{"end",			TokenCode.end_of_body},
				{"if",			TokenCode.if_statement},
				{"then",		TokenCode.then_statement},
				{"else",		TokenCode.else_statement},
				{"loop",		TokenCode.loop_start},
				{"while",		TokenCode.while_statement},
				{"for",			TokenCode.for_statement},
				{"in",			TokenCode.in_statement},
				{"reverse",		TokenCode.reverse_statement},
				{"print",		TokenCode.print_routine},
				{"routine", 	TokenCode.routine_declaration},
				{"return",		TokenCode.return_statement},
				{"break",		TokenCode.break_statement},
				{"continue",	TokenCode.continue_statement},
			};

		public static bool Keys(string key) =>
			dedicatedWords.ContainsKey(key);

		public static bool Vals(TokenCode code) =>
			dedicatedWords.ContainsValue(code);

		public static TokenCode Code(string key) =>
			dedicatedWords[key];
	}

	public static class SeparatorList {
		private static Dictionary<char, TokenCode> separatorCodes =
			new Dictionary<char, TokenCode>() {
				{',', TokenCode.comma},
				{'(', TokenCode.left_parenthesis},
				{')', TokenCode.right_parenthesis},
				{'[', TokenCode.left_bracket},
				{']', TokenCode.right_bracket},
				{'*', TokenCode.factor_op},
				{'%', TokenCode.factor_op},
				{'+', TokenCode.term_op}, 
				{'-', TokenCode.term_op},
				{';', TokenCode.semicolon},
			};

		public static bool Contains(char elem) =>
			separatorCodes.Keys.Contains(elem);

		public static TokenCode Code(char ch) =>
			separatorCodes[ch];
	}

	public class Token {
		private Position position {get;}
		private TokenCode tokenCode {get;}
		private object value {get; set;}

		public Position Position() => this.position;
		public TokenCode Code() => this.tokenCode;
		public object Value() => this.value;

		public void Value(object val) => this.value = val;

		// Construction
		public Token(Position pos, TokenCode code) {
			this.position = pos;
			this.tokenCode = code;
			this.value = "";
		}
		public Token(Position pos, TokenCode code, object val) {
			this.position = pos;
			this.tokenCode = code;
			this.value = val;
		}

		public void PrintInfo() {
			Console.Write($"Token of type {this.tokenCode}\tin position ({this.position.Row()}, {this.position.Col()}).\t");
			if(!(this.value is null))
				Console.Write($"Value in Token is {this.value}.");
			Console.Write("\n");
		}
	}
}
