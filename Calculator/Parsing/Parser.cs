using System;
using System.Collections.Generic;
namespace Calculator;

public class ParserException(string commandLine, string message, int index) : Exception {
    private string commandLine = commandLine;
    private string message = message;
    private int index = index;

    // I will make this handle printing to stderr on its own, bc FUCK IT
    public void PrettyPrint() {
        Console.Error.WriteLine($"\x1b[1;95mat index {index}:\x1b[m {message}\n\tINSERT HIGHLIGHTING");
    }
}

public class ParserExceptionFactory(string commandLine) {
    private string commandLine = commandLine;

    public ParserException MakeException(string message, int index) {
        return new ParserException(commandLine, message, index);
    }
}

// TODO: Rewrite this code to look at operator lexemes based on the lexeme ID instead of the actual
// token that they hold.
public static class Parser
{
    private struct EndTestResult(Nullable<Lexeme> lexeme, bool endOfStream)
    {
        public Nullable<Lexeme> Lexeme { get; } = lexeme;
        public bool EndOfStream { get; } = endOfStream;

        public static EndTestResult Ye(Lexeme lexeme) => new EndTestResult(lexeme, false);
        public static EndTestResult StreamEnd() => new EndTestResult(null, true);
        public static EndTestResult Nah() => new EndTestResult(null, false);
    }

    private static IExpression Merge(
        IExpression? left, IExpression right, LexemeID? op,
        Func<IExpression, IExpression, LexemeID?, IExpression> logic
    ) => left switch
    {
        null => right,
        _ => logic(left, right, op),
    };

    private static IExpression MergeMult(IExpression left, IExpression right, LexemeID? op) =>
        op switch
        {
            LexemeID.Multiply => new Multiply(left, right),
            LexemeID.Divide => new Divide(left, right),
            _ => throw new NotImplementedException("Not multiply or divide here"),
        };

    private static IExpression MergeAdd(IExpression left, IExpression right, LexemeID? op) =>
        op switch
        {
            LexemeID.Add => new Add(left, right),
            LexemeID.Subtract => new Subtract(left, right),
            _ => throw new NotImplementedException("Not add or subtract here"),
        };

    private static IExpression MaybeRecurse(IEnumerator<Lexeme> tokens, uint depth)
    {
        return tokens.Current.ID == LexemeID.IncPrecedence ?
            ParseRec(tokens, (tokens) => tokens.MoveNext() switch
            {
                true => tokens.Current.ID == LexemeID.DecPrecedence ? EndTestResult.Nah() : EndTestResult.Ye(tokens.Current),
                false => EndTestResult.StreamEnd(),
            }, depth + 1) : new Number(tokens.Current.token);
    }

    private static IExpression ParseRec(IEnumerator<Lexeme> tokens, Func<IEnumerator<Lexeme>, EndTestResult> tryNext, uint depth)
    {
        (IExpression? left, IExpression? mid, IExpression? right) = (null, null, null);
        LexemeID? oldAddOperator = null;
        LexemeID? oldMultOperator = null;

        if (!tryNext(tokens).Lexeme.HasValue)
        {
            throw new NotImplementedException("TODO: Implement error for no expression passed");
        }

        right = MaybeRecurse(tokens, depth);
        EndTestResult checkLexeme = EndTestResult.Nah(); // just initialize with *something* idfk

        // read two tokens at a time, first one should be an operator, second should be a number
        while ((checkLexeme = tryNext(tokens)).Lexeme.HasValue)
        {
            LexemeID op = tokens.Current.ID;
            if (!tryNext(tokens).Lexeme.HasValue)
            {
                throw new NotImplementedException("TODO: Implement unbalanced expression error");
            }

            IExpression operand = MaybeRecurse(tokens, depth);

            switch (op)
            {
                case LexemeID.Exponent:
                    right = new Exponent(right, operand);
                    break;
                case LexemeID.Multiply:
                case LexemeID.Divide:
                    mid = Merge(mid, right, oldMultOperator, MergeMult);
                    oldMultOperator = op;
                    right = operand;
                    break;
                case LexemeID.Add:
                case LexemeID.Subtract:
                    mid = Merge(mid, right, oldMultOperator, MergeMult);
                    right = operand;
                    oldMultOperator = null;
                    left = Merge(left, mid, oldAddOperator, MergeAdd);
                    oldAddOperator = op;
                    // mid = new Number(operand);
                    mid = null;
                    break;
                default:
                    throw new NotImplementedException("brooooooo wtf is this operator LMAOOO");
            }
        }

        if (checkLexeme.EndOfStream && depth > 0)
        {
            throw new NotImplementedException("Unbalanced parentheses");
        }

        mid = Merge(mid, right, oldMultOperator, MergeMult);
        return (left, mid, oldAddOperator) switch
        {
            (null, null, _) => throw new Exception("All are null. How???"),
            (null, _, _) => mid,
            (_, null, _) => left,
            (_, _, LexemeID.Add) => new Add(left, mid),
            (_, _, LexemeID.Subtract) => new Subtract(left, mid),
            _ => throw new Exception("meow :3"),
        };
    }

    // TODO: This parser ignores the lexer's identifiers of what kind of lexeme each element is.
    // I'll want to rewrite this to make it actually use that information.
    // TODO: Clean up parser code...
    public static IExpression Parse(IEnumerable<Lexeme> tokens)
    {
        using (var enumerator = tokens.GetEnumerator())
        {
            return ParseRec(enumerator, (tokens) => tokens.MoveNext() switch
            {
                true => EndTestResult.Ye(tokens.Current),
                false => EndTestResult.StreamEnd(),
            }, 0);
        }
    }
}

public class YourMom {
    private string status = "fat";
    public void PrintStatus() => Console.WriteLine($"yo mama so {status}");
}

