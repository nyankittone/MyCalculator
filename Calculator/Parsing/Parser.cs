using System;
using System.Collections.Generic;
namespace Calculator;

// Should I feel dirty for using `internal` here?
public class ParserException(string commandLine, string message, int index, int length) : Exception
{
    private string internalMessage = message;
    internal string CommandLine { get; } = commandLine;
    internal int Index { get; } = index;
    internal int Length { get; } = length;
    public override string Message
    {
        get => $"at index {Index}: {internalMessage}";
    }
}

public struct ParserExceptionFactory(string commandLine)
{
    private string commandLine = commandLine;

    public ParserException MakeException(string message, int index, int length)
    {
        return new ParserException(commandLine, message, index, length);
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

    // This function returns `null` on error, and should never throw an exception. I find this fine
    // since there's only 1 obvious way this thing can fail. Is this idiomatic C#? I don't think so,
    // but it makes sense to me,,,
    private static IExpression? Merge(IExpression? left, IExpression right, LexemeID? op) =>
        left switch
        {
            null => right,
            _ => op switch
            {
                LexemeID.Add => new Add(left, right),
                LexemeID.Subtract => new Subtract(left, right),
                LexemeID.Multiply => new Multiply(left, right),
                LexemeID.Divide => new Divide(left, right),
                LexemeID.Exponent => new Exponent(left, right),
                _ => null,
            },
        };

    private struct ParserStuff(ParserExceptionFactory maker)
    {
        public ParserExceptionFactory ParserMaker { get; } = maker;
        public List<ParserException> Errors { get; } = new();
    }

    private static IExpression MaybeRecurse(IEnumerator<Lexeme> tokens, ref ParserStuff stuff, uint depth)
    {
        return tokens.Current.ID == LexemeID.IncPrecedence ?
            ParseRec(tokens, ref stuff, (tokens) => tokens.MoveNext() switch
            {
                true => tokens.Current.ID == LexemeID.DecPrecedence ? EndTestResult.Nah() : EndTestResult.Ye(tokens.Current),
                false => EndTestResult.StreamEnd(),
            }, depth + 1) : new Number(tokens.Current.token);
    }

    private static IExpression ParseRec(
        IEnumerator<Lexeme> tokens,
        ref ParserStuff stuff,
        Func<IEnumerator<Lexeme>, EndTestResult> tryNext,
        uint depth)
    {
        (IExpression? left, IExpression? mid, IExpression? right) = (null, null, null);
        LexemeID? oldAddOperator = null;
        LexemeID? oldMultOperator = null;

        if (!tryNext(tokens).Lexeme.HasValue)
        {
            throw new NotImplementedException("TODO: Implement error for no expression passed");
        }

        right = MaybeRecurse(tokens, ref stuff, depth);
        EndTestResult checkLexeme = EndTestResult.Nah(); // just initialize with *something* idfk

        // read two tokens at a time, first one should be an operator, second should be a number
        while ((checkLexeme = tryNext(tokens)).Lexeme.HasValue)
        {
            LexemeID op = tokens.Current.ID;
            if (!tryNext(tokens).Lexeme.HasValue)
            {
                throw new NotImplementedException("TODO: Implement unbalanced expression error");
            }

            IExpression operand = MaybeRecurse(tokens, ref stuff, depth);

            switch (op)
            {
                case LexemeID.Exponent:
                    right = new Exponent(right, operand);
                    break;
                case LexemeID.Multiply:
                case LexemeID.Divide:
                    mid = Merge(mid, right, oldMultOperator);
                    oldMultOperator = op;
                    right = operand;
                    break;
                case LexemeID.Add:
                case LexemeID.Subtract:
                    mid = Merge(mid, right, oldMultOperator);
                    right = operand;
                    oldMultOperator = null;
                    left = Merge(left, mid, oldAddOperator);
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

        mid = Merge(mid, right, oldMultOperator);
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

    public static IExpression Parse(IEnumerable<Lexeme> tokens, ParserExceptionFactory errorMaker)
    {
        ParserStuff stuff = new();

        using (var enumerator = tokens.GetEnumerator())
        {
            return ParseRec(enumerator, ref stuff, (tokens) => tokens.MoveNext() switch
            {
                true => EndTestResult.Ye(tokens.Current),
                false => EndTestResult.StreamEnd(),
            }, 0);
        }
    }
}

public class YourMom
{
    private string status = "fat";
    public void PrintStatus() => Console.WriteLine($"yo mama so {status}");
}

