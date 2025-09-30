using LexicalAnalyzer.TokenTree;

// Namespace name manageable
namespace SyntaxAnalyzer
{
    // Syntax Analyzer class name manageable
    public class TreeSyntaxAnalyzer
    {
        public Queue<Token> tokenStream;
        public List<Node> childs;
        public TreeSyntaxAnalyzer(Queue<Token> tokenStream)
        {
            this.tokenStream = tokenStream;
            this.childs = new List<Node>();
        }

        public void ParseStream()
        {
            // Return type manageable            
        }

    }

    public class Node
    {
        public Token token;
        public List<Node> childs;
        public Node(Token token)
        {
            this.token = token;
            this.childs = new List<Node>();
        }
    }
}