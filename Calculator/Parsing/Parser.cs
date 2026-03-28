using System;
using System.Collections.Generic;
namespace Calculator;

// TODO: Add ErrorID field to ParserException and BasicParserException, and have the Message field
// get computed based on that ID

public enum ParserErrorID
{
    ExpectedNumber,
    ExpectedOperator,
    EmptyParenthesis,
    UnbalancedOperator,
    UnclosedParenthesis,
    ExtraParenthesisClose,
    InvalidOperator,
    Other,
}

public static class ParserErrorIDExt
{
    public static string MakeMessage(this ParserErrorID ID, ref SequentialLexeme? lexeme) => (ID, lexeme) switch
    {
        (ParserErrorID.ExpectedNumber, null) => "Expected number or open parenthesis",
        (ParserErrorID.ExpectedNumber, SequentialLexeme l) => $"Expected number or open parenthesis, got {l.ID}",
        (ParserErrorID.ExpectedOperator, null) => "Expected operator",
        (ParserErrorID.ExpectedOperator, SequentialLexeme l) => $"Expected operator, got {l.ID}",
        (ParserErrorID.EmptyParenthesis, _) => "Empty parenthesis",
        (ParserErrorID.UnbalancedOperator, SequentialLexeme l) => $"Unbalanced operator \"{l.ID}\"",
        (ParserErrorID.UnbalancedOperator, _) => "Unbalanced operator",
        (ParserErrorID.UnclosedParenthesis, _) => "Unclosed parenthesis",
        (ParserErrorID.InvalidOperator, SequentialLexeme l) => $"Invalid operator \"{l.Token}\"",
        (ParserErrorID.InvalidOperator, _) => "Invalid operator",
        (ParserErrorID.ExtraParenthesisClose, _) => "Cannot have a closing parenthesis here",
        _ => "Unknown error type",
    };
}

public class ParserException(string commandLine, SequentialLexeme? lexeme, ParserErrorID id) : Exception
{
    public ParserErrorID ID { get; } = id;
    private SequentialLexeme? lexeme = lexeme;
    public string CommandLine { get; } = commandLine;
    public int? Index
    {
        get => lexeme switch
        {
            SequentialLexeme lex => lex.Index,
            null => null,
        };
    }
    public int? Length
    {
        get => lexeme switch
        {
            SequentialLexeme lexx => lexx.Token.Length,
            null => null,
        };
    }
    public override string Message
    {
        get
        {
            string messagePart = ID.MakeMessage(ref lexeme);
            return lexeme switch
            {
                SequentialLexeme l => l.Index switch
                {
                    int idx => $"at index {idx + 1}: {messagePart}",
                    _ => $"at index <unknown>: {messagePart}",
                },
                null => $"at index <unknown>: {messagePart}",
            };
        }
    }
}

public struct ParserExceptionFactory(string commandLine)
{
    private string commandLine = commandLine;

    public ParserException MakeException(ParserErrorID id, SequentialLexeme lexeme) =>
        new ParserException(commandLine, lexeme, id);

    public ParserException MakeException(ParserErrorID id) =>
        new ParserException(commandLine, null, id);
}

// TODO: Rewrite this code to look at operator lexemes based on the lexeme ID instead of the actual
// token that they hold.
public static class Parser
{
    private struct EndTestResult(SequentialLexeme? lexeme, bool endOfStream)
    {
        public SequentialLexeme? Lexeme { get; } = lexeme;
        public bool EndOfStream { get; } = endOfStream;

        public static EndTestResult Ye(SequentialLexeme lexeme) => new EndTestResult(lexeme, false);
        public static EndTestResult StreamEnd() => new EndTestResult(null, true);
        public static EndTestResult Nah() => new EndTestResult(null, false);
    }

    // This function returns `null` on error, and should never throw an exception. I find this fine
    // since there's only 1 obvious way this thing can fail. Is this idiomatic C#? I don't think so,
    // but it makes sense to me,,,
    private static IExpression? Merge(IExpression? left, IExpression? right, LexemeID? op) =>
        right switch
        {
            null => null,
            _ =>
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
                        _ => throw new ArgumentException("Invalid operator lexeme here"),
                    },
                },
        };

    private struct ParserStuff
    {
        public ParserExceptionFactory ErrorMaker { get; }
        public List<Exception> Errors { get; } = new();

        [Obsolete("Parameterless constructor called by mistake", true)]
        public ParserStuff() { }

        public ParserStuff(ParserExceptionFactory maker)
        {
            ErrorMaker = maker;
        }
    }

    private static IExpression? MaybeRecurse(IEnumerator<SequentialLexeme> tokens, ref ParserStuff stuff, uint depth)
    {
        return tokens.Current.ID == LexemeID.IncPrecedence ?
            ParseRec(tokens, ref stuff, (tokens) => tokens.MoveNext() switch
            {
                true => tokens.Current.ID == LexemeID.DecPrecedence ? EndTestResult.Nah() : EndTestResult.Ye(tokens.Current),
                false => EndTestResult.StreamEnd(),
            }, depth + 1) : new Number(tokens.Current.Token);
    }

    private static IExpression? TryParseNumber(IEnumerator<SequentialLexeme> tokens, ref ParserStuff stuff, uint depth)
    {
        SequentialLexeme openParenthLexeme = tokens.Current;
        IExpression? returned = MaybeRecurse(tokens, ref stuff, depth);
        if (returned is null)
        {
            try {
            if (tokens.Current.SeqIndex - openParenthLexeme.SeqIndex < 2)
            {
                stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.EmptyParenthesis, openParenthLexeme));
            }
            } catch(InvalidOperationException) {
                stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.UnclosedParenthesis, openParenthLexeme));
            }
        }

        return returned;
    }

    // `true` is returned when we return without consuming the entire iterator, `false` otherwise.
    private static bool FindValidOperand(
        IEnumerator<SequentialLexeme> tokens,
        ref ParserStuff stuff,
        Func<IEnumerator<SequentialLexeme>, EndTestResult> tryNext
        )
    {
        bool wasInvalid = false;

        while (true)
        {
            if (!tryNext(tokens).Lexeme.HasValue)
            {
                return true;
            }

            var lexeme = tokens.Current;

            if (lexeme.ID == LexemeID.Invalid)
            {
                stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.ExpectedNumber, lexeme));
                wasInvalid = true;
                continue;
            }

            if (lexeme.ID == LexemeID.DecPrecedence)
            {
                stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.ExpectedNumber, lexeme));
                stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.ExtraParenthesisClose, lexeme));
                wasInvalid = true;
                continue;
            }

            if (lexeme.IsOperator())
            {
                if (wasInvalid)
                {
                    wasInvalid = false;
                }
                else
                {
                    stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.ExpectedNumber, lexeme));
                }
                continue;
            }

            break;
        }

        return false;
    }

    // I hate this type
    private enum FindOperatorReturn
    {
        CurrentIsNumber,
        CurrentIsOperator,
        OutOfLexemes,
    }

    private static FindOperatorReturn FindValidOperator(
        IEnumerator<SequentialLexeme> tokens,
        ref ParserStuff stuff,
        Func<IEnumerator<SequentialLexeme>, EndTestResult> tryNext
    )
    {
        SequentialLexeme lexeme = tokens.Current;
        if (!lexeme.IsOperator())
        {
            stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.ExpectedOperator, lexeme));
            if (lexeme.ID == LexemeID.DecPrecedence)
            {
                stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.ExtraParenthesisClose, lexeme));
            }

            while (tryNext(tokens).Lexeme.HasValue)
            {
                lexeme = tokens.Current;
                if (lexeme.ID == LexemeID.Number || lexeme.ID == LexemeID.IncPrecedence)
                {
                    return FindOperatorReturn.CurrentIsNumber;
                }

                if (lexeme.IsOperator())
                {
                    return FindOperatorReturn.CurrentIsOperator;
                }

                stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.ExpectedOperator, lexeme));
                if (lexeme.ID == LexemeID.DecPrecedence)
                {
                    stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.ExtraParenthesisClose, lexeme));
                }
            }

            // we are here if we run out of lexemes to chew through
            return FindOperatorReturn.OutOfLexemes;
        }

        return FindOperatorReturn.CurrentIsOperator;
    }

    private static IExpression? ParseRec(
        IEnumerator<SequentialLexeme> tokens,
        ref ParserStuff stuff,
        Func<IEnumerator<SequentialLexeme>, EndTestResult> tryNext,
        uint depth)
    {
        (IExpression? left, IExpression? mid, IExpression? right) = (null, null, null);
        LexemeID? oldAddOperator = null;
        LexemeID? oldMultOperator = null;

        if (FindValidOperand(tokens, ref stuff, tryNext))
        {
            return null;
        }

        // This right here is the first token to actually make any sense. Ensure that it's
        // either a number or some expression surrounded by parenthesis, and if so, if the
        // parenthesis contain anything.
        if (TryParseNumber(tokens, ref stuff, depth) is IExpression resolved)
        {
            right = resolved;
        }

        EndTestResult checkLexeme = EndTestResult.Nah(); // just initialize with *something* idfk

        // read two tokens at a time, first one should be an operator, second should be a number
        while ((checkLexeme = tryNext(tokens)).Lexeme.HasValue)
        {
            // We want to make it so that detecting the operator will stop if we encounter any
            // number of garbage lexemes, followed by a number or opening parenthesis. This will
            // require on the case of this sequence of lexemes occuring, that we skip finding the
            // next operand, since we already know the next operand, as well as skipping switching
            // on the operator, since we know it's invalid.
            SequentialLexeme? op = null;
            switch (FindValidOperator(tokens, ref stuff, tryNext))
            {
                case FindOperatorReturn.CurrentIsOperator:
                    op = tokens.Current;
                    break;
                case FindOperatorReturn.CurrentIsNumber:
                    TryParseNumber(tokens, ref stuff, depth);
                    continue;
                case FindOperatorReturn.OutOfLexemes:
                    continue;
            }

            int oldErrorCount = stuff.Errors.Count;
            if (FindValidOperand(tokens, ref stuff, tryNext))
            {
                if (oldErrorCount == stuff.Errors.Count)
                {
                    stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.UnbalancedOperator, op.Value));
                }
                continue;
            }

            IExpression? operand = TryParseNumber(tokens, ref stuff, depth);

            switch (op.Value.ID)
            {
                case LexemeID.Exponent:
                    if (operand is IExpression _ && right is IExpression _)
                    {
                        right = new Exponent(right, operand);
                    }
                    break;
                case LexemeID.Multiply:
                case LexemeID.Divide:
                    mid = Merge(mid, right, oldMultOperator);
                    oldMultOperator = op.Value.ID; // these two lines might have to be inside the above if
                    right = operand;
                    break;
                case LexemeID.Add:
                case LexemeID.Subtract:
                    mid = Merge(mid, right, oldMultOperator);
                    right = operand;
                    oldMultOperator = null;
                    left = Merge(left, mid, oldAddOperator);
                    oldAddOperator = op.Value.ID;
                    mid = null;
                    break;
                default:
                    stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.InvalidOperator, op.Value));
                    break;
            }
        }

        if (checkLexeme.EndOfStream && depth > 0)
        {
            Console.Error.WriteLine("MEOWWWWWWWW <3");
            // TODO: Save the beginning lexeme for the open parenthesis for use in these errors
            stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.UnclosedParenthesis, checkLexeme.Lexeme.Value));
            Console.Error.WriteLine(":3 <3");
        }

        mid = Merge(mid, right, oldMultOperator);
        return (left, mid, oldAddOperator) switch
        {
            (null, null, _) => null,
            (null, _, _) => mid,
            (_, null, _) => left,
            (_, _, LexemeID.Add) => new Add(left, mid),
            (_, _, LexemeID.Subtract) => new Subtract(left, mid),
            _ => throw new Exception("meow :3"),
        };
    }

    public static IExpression? Parse(IEnumerable<SequentialLexeme> tokens, ParserExceptionFactory errorMaker)
    {
        ParserStuff stuff = new(errorMaker);

        using (var enumerator = tokens.GetEnumerator())
        {
            IExpression? expr = ParseRec(enumerator, ref stuff, (tokens) => tokens.MoveNext() switch
            {
                true => EndTestResult.Ye(tokens.Current),
                false => EndTestResult.StreamEnd(),
            }, 0);

            if (stuff.Errors.Count > 0)
            {
                throw new AggregateException(stuff.Errors);
            }

            return expr;
        }
    }
}

