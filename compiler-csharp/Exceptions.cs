using Data.Objects;

namespace Exceptions {
	public abstract class CompilerException : Exception {
		protected Position pos;

		protected CompilerException(Position pos, string msg) : base(msg) => this.pos = pos;

		public abstract void Info();
	}
	
	public class UnexpectedTokenException : CompilerException {
		public UnexpectedTokenException(Position pos) : base(pos, $"Unexpected token at {pos.Row()},{pos.Col()}.") { }

		public override void Info() =>
			Console.WriteLine(this.Message);
	}

	public abstract class InvalidSequenceBreakUsage : CompilerException{
		public InvalidSequenceBreakUsage(Position pos, string msg) : base(pos, msg) { }
	}

	public class BreakOutsideCycle : InvalidSequenceBreakUsage {
		BreakOutsideCycle(Position pos) : base(pos, $"Break used outside the loop body at position {pos.Row()},{pos.Col()}.") { }

		public override void Info() =>
			Console.WriteLine(this.Message);
	}

	public class ContinueOutsideCycle : InvalidSequenceBreakUsage {
		ContinueOutsideCycle(Position pos) : base(pos, $"Continue used outside the loop body at position {pos.Row()},{pos.Col()}.") { }

		public override void Info() =>
			Console.WriteLine(this.Message);
	}

	public class ReturnOutsideFunction : InvalidSequenceBreakUsage {
		ReturnOutsideFunction(Position pos) : base(pos, $"Return used outside the funtcion body at position {pos.Row()},{pos.Col()}.") { }

		public override void Info() =>
			Console.WriteLine(this.Message);
	}
}
