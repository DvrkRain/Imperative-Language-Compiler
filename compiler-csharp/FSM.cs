using System;
using System.Collections.Generic;
using System.Text;

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

String -- ["] --> завершение строки -> Start
String -- [\] --> экранирование следующего символа -> String
String -- [другой] --> String

Comment -- [конец строки] --> завершение комментария -> Start
Comment -- [другой] --> Comment
*/

namespace Lexer.FSM
{
    // Enum is needed to easy check state switching
    public enum LexerState
    {
        Start,
        Identifier,
        Number,
        Decimal,
        AssignmentOrColon,
        DotOrRange,
        Less,
        Greater,
        NotEqual,
        String,
        Comment,
        Error
    }

    public class FSM
    {
        private State currentState;
        private LexerState currentLexerState;
        private readonly Dictionary<LexerState, State> states = new Dictionary<LexerState, State>();

        public FSM()
        {
            // Fill start 
            this.currentState = new StartState();
            this.currentLexerState = LexerState.Start;

            // Initialize states
            states.Add(LexerState.Start, new StartState());
            // states.Add(LexerState.Identifier, new IdentifierState());
            // states.Add(LexerState.Number, new NumberState());
            // states.Add(LexerState.Decimal, new DecimalState());
            // states.Add(LexerState.AssignmentOrColon, new AssignmentOrColonState());
            // states.Add(LexerState.DotOrRange, new DotOrRangeState());
            // states.Add(LexerState.Less, new LessState());
            // states.Add(LexerState.Greater, new GreaterState());
            // states.Add(LexerState.NotEqual, new NotEqualState());
            // states.Add(LexerState.String, new StringState());
            // states.Add(LexerState.Comment, new CommentState());
            // states.Add(LexerState.Error, new ErrorState());
        }

        public void ProcessChar(char c)
        {
            LexerState nextLexerState = this.currentState.HandleSymbol(c);

            while (nextLexerState != this.currentLexerState) // State changed
            {
                // If state changed, then state data keeps the same without adding `c`
                // Take data and tokenize it
                string data = this.currentState.data;
                // tokenize

                // Switch states
                this.currentState = this.states[nextLexerState];
                this.currentLexerState = nextLexerState;

                if (this.currentLexerState == LexerState.Error) throw new Exception("Error happened(");

                // Handle character
                nextLexerState = this.currentState.HandleSymbol(c);
            }

            // If state keeps the same (e.g. state `Number` handles '1' and keeps `Number`), chill :3

        }
    }

    public abstract class State
    {
        public string data { get; protected set; } = "";

        protected State() { }

        public abstract LexerState HandleSymbol(char symbol);
    }

    public class StartState : State
    {
        public override LexerState HandleSymbol(char symbol)
        {
            if (char.IsWhiteSpace(symbol)) return LexerState.Start;

            if (char.IsDigit(symbol)) return LexerState.Number;

            if (char.IsLetter(symbol)) return LexerState.Identifier; // Need to check

            if (symbol == ':') return LexerState.AssignmentOrColon;

            if (symbol == '.') return LexerState.DotOrRange;

            if (symbol == '<') return LexerState.Less;

            if (symbol == '>') return LexerState.Greater;

            if (symbol == '/') return LexerState.NotEqual;

            // ...

            return LexerState.Error;
        }
    }
}
