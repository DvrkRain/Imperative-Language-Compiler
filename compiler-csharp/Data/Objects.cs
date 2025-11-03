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
		record_decl,
		array_decl,
		// Variable declaration
		variable_decl,
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
		if_stnt,
		then_stnt,
		else_stnt,
		// Loops
		while_stnt,
		for_stnt,
		in_stnt,
		reverse_stnt,
		loop_start,
		range_sign,
		// Routines
		print_rtn,
		routine_decl,
		one_line_body,
		end_of_body,
	}

	public static class DedicatedWords {
		private static Dictionary<string,TokenCode> dedicatedWords =
			new Dictionary<string,TokenCode>() {
				{"integer", TokenCode.builtin_type},
				{"real",	TokenCode.builtin_type},
				{"boolean", TokenCode.builtin_type},
				{"record",	TokenCode.record_decl},
				{"array",	TokenCode.array_decl},
				{"and",		TokenCode.logic_op},
				{"or",		TokenCode.logic_op},
				{"xor",		TokenCode.logic_op},
				{"not",		TokenCode.logic_op},
				{"true",	TokenCode.constant_value},
				{"false",	TokenCode.constant_value},
				{"var",		TokenCode.variable_decl},
				{"is",		TokenCode.is_assignment},
				{"type",	TokenCode.type_declaration},
				{"end",		TokenCode.end_of_body},
				{"if",		TokenCode.if_stnt},
				{"then",	TokenCode.then_stnt},
				{"else",	TokenCode.else_stnt},
				{"loop",	TokenCode.loop_start},
				{"while",	TokenCode.while_stnt},
				{"for",		TokenCode.for_stnt},
				{"in",		TokenCode.in_stnt},
				{"reverse",	TokenCode.reverse_stnt},
				{"print",	TokenCode.print_rtn},
				{"routine", TokenCode.routine_decl},
			};

		public static bool Contains(string key) =>
			dedicatedWords.Keys.Contains(key);

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
		public Position pos;
		public TokenCode tokenCode;
		public object? value;

		// Construction
		// Basic
		private Token(int x, int y) =>
			this.pos = new Position(x,y);
		private Token(Position pos) =>
			this.pos = pos;
		// Without value
		public Token(int x, int y, TokenCode code) : this(x,y) =>
			this.tokenCode = code;
		public Token(Position pos, TokenCode code) : this(pos) =>
			this.tokenCode = code;
		// With value
		public Token(int x, int y, TokenCode code, object val) : this(x,y, code) =>
			this.value = val;
		public Token(Position pos, TokenCode code, object val) : this(pos, code) =>
			this.value = val;

		public void PrintInfo() {
			Console.Write($"Token of type {this.tokenCode}\tin position ({this.pos.Row()}, {this.pos.Col()}).\t");
			if(!(this.value is null))
				Console.Write($"Value in Token is {this.value}.");
			Console.Write("\n");
		}
	}
}
