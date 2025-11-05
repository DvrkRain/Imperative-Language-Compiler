using Data.Objects;

namespace Exceptions {
	public abstract class CompilerException : Exception {
		protected Position position;
		protected string invoker;

		protected CompilerException(Position pos, string invoker, string msg) : base(msg) {
			this.position = pos;
			this.invoker = invoker;
		}

		public void Info() =>
			Console.WriteLine(this.Message);
	}
	
	public class UnexpectedTokenException : CompilerException {
		public UnexpectedTokenException(Position pos, string invoker)
			: base(pos, invoker, $"Unexpected token at {pos.Row()},{pos.Col()}.") { }
	}


	// Sequence break exceptions
	public abstract class InvalidSequenceBreakUsage : CompilerException {
		public InvalidSequenceBreakUsage(Position pos, string invoker, string msg)
			: base(pos, invoker, msg) { }
	}

	public class BreakOutsideCycle : InvalidSequenceBreakUsage {
		BreakOutsideCycle(Position pos, string invoker)
			: base(pos, invoker, $"Break used outside the loop body at position {pos.Row()},{pos.Col()}.") { }
	}

	public class ContinueOutsideCycle : InvalidSequenceBreakUsage {
		ContinueOutsideCycle(Position pos, string invoker)
			: base(pos, invoker, $"Continue used outside the loop body at position {pos.Row()},{pos.Col()}.") { }
	}

	public class ReturnOutsideFunction : InvalidSequenceBreakUsage {
		ReturnOutsideFunction(Position pos, string invoker)
			: base(pos, invoker, $"Return used outside the funtcion body at position {pos.Row()},{pos.Col()}.") { }
	}


	public class UnexpectedEndOfFile : CompilerException {
		public UnexpectedEndOfFile(Position pos, string invoker)
			: base(pos, invoker, $"End of file reached unexpectedly in command at position {pos.Row()},{pos.Col()}") { }
	}
}
