using System;
using System.Collections.Generic;
namespace Calculator;

public enum ParserErrorID
{
    ExpectedNumber,
    ExpectedOperator,
    EmptyParenthesis,
    UnbalancedOperator,
    UnclosedParenthesis,
    ExtraParenthesisClose,
    InvalidOperator,
    WrongArgumentCount,
    Other,
}

public static class ParserErrorIDExtensions
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
        // TODO: This bottom error is really not very useful. We need to somehow extend this type
        // with more info...
        (ParserErrorID.WrongArgumentCount, _) => "Function does not have the right number of arguments",
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

    private static IEnumerable<IExpression> CollectFunctionArgs(
        IEnumerator<SequentialLexeme> tokens,
        ref ParserStuff stuff,
        Func<IEnumerator<SequentialLexeme>, EndTestResult> tryNext,
        uint depth)
    {
        // For now, unitl I implement comma support in the lexer, this thing will only resolve 0 or
        // 1 arguments.

        if (tryNext(tokens).Lexeme.HasValue)
        {
            switch (tokens.Current.ID)
            {
                case LexemeID.Constant:
                case LexemeID.Variable:
                case LexemeID.Number:
                case LexemeID.BuiltinFunc:
                case LexemeID.CustomFunc:
                case LexemeID.IncPrecedence:
                    return TryParseNumber(tokens, ref stuff, tryNext, depth) switch
                    {
                        IExpression expr => [expr],
                        null => [new Number(0)],
                    };
                case LexemeID.Invalid:
                case LexemeID.DecPrecedence:
                    throw new NotImplementedException("Invalid token after function name");
                default: break;
            }
        }

        return [];
    }

    // tries to call something that it thinks is a function, and returns an appropriate IExpression
    // if successful.
    private static (IExpression?, bool) TryCallFunction(
        IEnumerator<SequentialLexeme> tokens,
        ref ParserStuff stuff,
        Func<IEnumerator<SequentialLexeme>, EndTestResult> tryNext,
        uint depth)
    {
        // get the function parameter list
        // try to call the function
        // NOTE: Doing this may not work all that well if a custom function is used twice in one
        // expression, assuming we use the same custom function instance.
        string functionName = tokens.Current.Token; // This feels gross :(
        IEnumerable<IExpression> args = CollectFunctionArgs(tokens, ref stuff, tryNext, depth);
        try
        {
            return (SymbolFinder.Singleton.GetExprFromFunc(functionName, args), false);
        }
        catch (ArgumentException e)
        {
            stuff.Errors.Add(e); // TODO: Create a ParserException from this!
        }

        return (null, false);
    }

    private static (IExpression?, bool) MaybeRecurse(
        IEnumerator<SequentialLexeme> tokens,
        ref ParserStuff stuff,
        Func<IEnumerator<SequentialLexeme>, EndTestResult> tryNext,
        uint depth)
    => tokens.Current.ID switch
    {
        LexemeID.IncPrecedence =>
            ParseRec(tokens, ref stuff, (tokens) => tokens.MoveNext() switch
                    {
                        true => tokens.Current.ID == LexemeID.DecPrecedence ? EndTestResult.Nah() : EndTestResult.Ye(tokens.Current),
                        false => EndTestResult.StreamEnd(),
                    }, depth + 1),
        LexemeID.Constant or LexemeID.Variable => (
            new Number(SymbolFinder.Singleton.GetFalliableNumber(tokens.Current.Token)),
            false
        ),
        LexemeID.Number => (new Number(tokens.Current.Token), false),
        LexemeID.BuiltinFunc or LexemeID.CustomFunc => TryCallFunction(tokens, ref stuff, tryNext, depth),
        _ => throw new ArgumentException("Invalid lexeme type for MaybeRecurse()"),
    };

    // Wraps around MaybeRecurse, and on failure, tries to add errors to the error list.
    private static IExpression? TryParseNumber(
            IEnumerator<SequentialLexeme> tokens,
            ref ParserStuff stuff,
            Func<IEnumerator<SequentialLexeme>, EndTestResult> tryNext,
            uint depth)
    {
        SequentialLexeme openParenthLexeme = tokens.Current;
        (IExpression? returned, bool openParenthReported) = MaybeRecurse(tokens, ref stuff, tryNext, depth);
        if (returned is null)
        {
            try
            {
                // If this condition is true, that means we have an empty parenthesis block
                if (tokens.Current.SeqIndex - openParenthLexeme.SeqIndex < 2)
                {
                    stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.EmptyParenthesis, openParenthLexeme));
                }
            }
            // This should get caught if the check in the try{} throws when referring to
            // tokens.Current. This indicates that a parenthesis has not been closed.
            catch (InvalidOperationException)
            {
                if (!openParenthReported)
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

    private static (IExpression?, bool) ParseRec(
        IEnumerator<SequentialLexeme> tokens,
        ref ParserStuff stuff,
        Func<IEnumerator<SequentialLexeme>, EndTestResult> tryNext,
        uint depth)
    {
        bool openParenthReported = false;

        try
        {
            (IExpression? left, IExpression? mid, IExpression? right) = (null, null, null);
            LexemeID? oldAddOperator = null;
            LexemeID? oldMultOperator = null;

            SequentialLexeme? openLexeme = depth > 0 ? tokens.Current : null;

            if (FindValidOperand(tokens, ref stuff, tryNext))
            {
                return (null, false);
            }

            // This right here is the first token to actually make any sense. Ensure that it's
            // either a number or some expression surrounded by parenthesis, and if so, if the
            // parenthesis contain anything.
            if (TryParseNumber(tokens, ref stuff, tryNext, depth) is IExpression resolved)
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
                        TryParseNumber(tokens, ref stuff, tryNext, depth);
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

                IExpression? operand = TryParseNumber(tokens, ref stuff, tryNext, depth);

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
                stuff.Errors.Add(stuff.ErrorMaker.MakeException(ParserErrorID.UnclosedParenthesis, openLexeme.Value));
                openParenthReported = true;
            }

            mid = Merge(mid, right, oldMultOperator);
            return (left, mid, oldAddOperator) switch
            {
                (null, null, _) => (null, openParenthReported),
                (null, _, _) => (mid, openParenthReported),
                (_, null, _) => (left, openParenthReported),
                (_, _, LexemeID.Add) => (new Add(left, mid), openParenthReported),
                (_, _, LexemeID.Subtract) => (new Subtract(left, mid), openParenthReported),
                _ => throw new ArgumentException("Invalid operator specified for final merge"),
            };
        }
        catch (Exception e) when (e is (ArgumentException or NullReferenceException or ArithmeticException or FormatException))
        {
            stuff.Errors.Add(e);

            // Try to zoom to the end of the expression. This might fail
            try
            {
                while (tryNext(tokens).Lexeme is not null) ;
            }
            catch (Exception e2)
            {
                throw new ArgumentException("Failed to scroll to end of parenthesis block!", e2);
            }

            return (null, openParenthReported);
        }
    }

    public static IExpression? Parse(IEnumerable<SequentialLexeme> tokens, ParserExceptionFactory errorMaker)
    {
        ParserStuff stuff = new(errorMaker);

        using (var enumerator = tokens.GetEnumerator())
        {
            IExpression? expr = null;

            try
            {
                (IExpression? tmpExpr, _) = ParseRec(enumerator, ref stuff, (tokens) => tokens.MoveNext() switch
                {
                    true => EndTestResult.Ye(tokens.Current),
                    false => EndTestResult.StreamEnd(),
                }, 0);

                expr = tmpExpr;
            }
            catch (StackOverflowException e)
            {
                stuff.Errors.Add(new StackOverflowException("Expression is too deep!", e));
            }
            catch (Exception e) when (e is ArgumentException)
            {
                stuff.Errors.Add(e);
            }

            if (stuff.Errors.Count > 0)
            {
                throw new AggregateException(stuff.Errors);
            }

            return expr;
        }
    }
}

