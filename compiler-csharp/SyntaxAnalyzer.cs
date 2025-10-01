using LexicalAnalyzer.TokenTree;

namespace SyntaxAnalyzer
{
    public abstract class Node
    {
        protected List<Node> childs;

        public Node() => this.childs = new List<Node>();

        public abstract void Parse(ref Queue<Token> tokenQueue);
    }

    public class ProgramNode : Node
    {
        public bool main;

        public ProgramNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class IfNode : Node
    {
        public List<BranchNode> branches;

        public IfNode() : base() => this.branches = new List<BranchNode>();

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public abstract class IdentifierNode : Node
    {
        protected string identifier;

        public IdentifierNode() : base() => this.identifier = "";
    }

    public class VarNode : IdentifierNode
    {
        public VarNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {} 
    }

    public class TypeNode : IdentifierNode
    {
        public TypeNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class RecordNode : IdentifierNode
    {
        public RecordNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class ArrayNode : IdentifierNode
    {
        public ArrayNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class AssignmentNode : IdentifierNode
    {
        public AssignmentNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public abstract class SubprogramNode : Node
    {
        protected ProgramNode nested;

        public SubprogramNode() : base() {}
    }

    public class ForNode : SubprogramNode
    {
        public ForNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class WhileNode : SubprogramNode
    {
        public WhileNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }

    public class BranchNode : SubprogramNode
    {
        public BranchNode() : base() { }

        public override void Parse(ref Queue<Token> tokenQueue) {}
    }
}