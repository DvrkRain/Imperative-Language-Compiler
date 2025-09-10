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

    public abstract class State
    {
        public string data { get; protected set; } = "";

        protected State(char? symbol = null)
        {
            if (symbol != null)
            {
                data += symbol;
            }
        }

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
