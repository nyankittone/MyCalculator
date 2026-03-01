// TODO: Add testing.
// TODO: Add error handling in the tokenizer and parser.
// TODO: Add support for pre-defined math functions, i.e. sqrt, floor, ciel, min, max, etc.
// TODO: Add support for defining custom functions.
// TODO: Use arbitrary-precision numbers instead of the built-in `decimal` type.

using System;
using System.Collections.Generic;
using System.Linq;
using RE = System.Text.RegularExpressions;
namespace Calculator;

public enum LexemeID
{
    Number,
    Operator,
    Add,
    Subtract,
    Multiply,
    Divide,
    IncPrecedence,
    DecPrecedence,
    Func,
}

public struct Lexeme
{
    public LexemeID ID { get; }
    public string token { get; }

    private Lexeme(LexemeID ID, string token)
    {
        this.ID = ID;
        this.token = token;
    }

    public override string ToString()
    {
        return $"{ID}({token})";
    }

    public static Lexeme Number(string token) => new Lexeme(LexemeID.Number, token);
    public static Lexeme Operator(string token) => new Lexeme(LexemeID.Operator, token);
    public static Lexeme IncPrecedence(string token) => new Lexeme(LexemeID.IncPrecedence, token);
    public static Lexeme DecPrecedence(string token) => new Lexeme(LexemeID.DecPrecedence, token);
}

// We're going to make the parser also take the role of the lexer, for convenience on my end bc I
// don't feel like being smart right now.
public interface IExpression
{
    public decimal Evaluate();
}

public class Number : IExpression
{
    private decimal number;
    public Number(string token)
    {
        number = Decimal.Parse(token); // TODO: Think about error handling here.
    }

    public decimal Evaluate() => number;
}

public abstract class Operator : IExpression
{
    protected IExpression left;
    protected IExpression right;

    public Operator(IExpression left, IExpression right)
    {
        this.left = left;
        this.right = right;
    }

    public abstract decimal Evaluate();
}

// I reeeeeally wish I didn't have to explicitly mention the constructor in every derived class.
// That's a little annoying.
public class Add : Operator
{
    public Add(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() + right.Evaluate();
}

public class Subtract : Operator
{
    public Subtract(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() - right.Evaluate();
}

public class Multiply : Operator
{
    public Multiply(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() * right.Evaluate();
}

public class Divide : Operator
{
    public Divide(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() / right.Evaluate();
}

public class Exponent : Operator
{
    public Exponent(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => (decimal)Math.Pow((double)left.Evaluate(), (double)right.Evaluate());
}

public class Sqrt : IExpression
{
    private IExpression unsquared;
    public Sqrt(IExpression expr)
    {
        unsquared = expr;
    }

    // grrrrrr I hate these casts
    public decimal Evaluate() => (decimal)Math.Sqrt((double)unsquared.Evaluate());
}

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
        IExpression? left, IExpression right, string? op,
        Func<IExpression, IExpression, string?, IExpression> logic
    ) => left switch
    {
        null => right,
        _ => logic(left, right, op),
    };

    private static IExpression MergeMult(IExpression left, IExpression right, string? op) =>
        op switch
        {
            "*" => new Multiply(left, right),
            "/" => new Divide(left, right),
            _ => throw new NotImplementedException("Not multiply or divide here"),
        };

    private static IExpression MergeAdd(IExpression left, IExpression right, string? op) =>
        op switch
        {
            "+" => new Add(left, right),
            "-" => new Subtract(left, right),
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
        string? oldAddOperator = null;
        string? oldMultOperator = null;

        if (!tryNext(tokens).Lexeme.HasValue)
        {
            throw new NotImplementedException("TODO: Implement error for no expression passed");
        }

        right = MaybeRecurse(tokens, depth);
        EndTestResult checkLexeme = EndTestResult.Nah(); // just initialize with *something* idfk

        // read two tokens at a time, first one should be an operator, second should be a number
        while ((checkLexeme = tryNext(tokens)).Lexeme.HasValue)
        {
            string operatorToken = tokens.Current.token;
            if (!tryNext(tokens).Lexeme.HasValue)
            {
                throw new NotImplementedException("TODO: Implement unbalanced expression error");
            }

            IExpression operand = MaybeRecurse(tokens, depth);

            // now what???
            switch (operatorToken)
            {
                case "**":
                    right = new Exponent(right, operand);
                    break;
                case "*":
                case "/":
                    mid = Merge(mid, right, oldMultOperator, MergeMult);
                    oldMultOperator = operatorToken;
                    right = operand;
                    break;
                case "+":
                case "-":
                    mid = Merge(mid, right, oldMultOperator, MergeMult);
                    right = operand;
                    oldMultOperator = null;
                    left = Merge(left, mid, oldAddOperator, MergeAdd);
                    oldAddOperator = operatorToken;
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
            (_, _, "+") => new Add(left, mid),
            (_, _, "-") => new Subtract(left, mid),
            (_, _, "*") => new Multiply(left, mid),
            (_, _, "/") => new Divide(left, mid),
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

public static class Lexer
{
    private static Nullable<int> CheckNumber(string input)
    {
        if (input.Length == 0)
        {
            return null;
        }

        int returned = 0;
        if (input[0] == '+' || input[0] == '-')
        {
            returned++;
        }

        RE.Match leftMatch = RE.Regex.Match(input[returned..], @"^\d+");
        if (leftMatch.Success)
        {
            returned += leftMatch.Length;
        }

        if (input[returned..].Length == 0 || input[returned] != '.')
        {
            return leftMatch.Success ? returned : null;
        }

        returned++;

        RE.Match rightMatch = RE.Regex.Match(input[returned..], @"^\d+");
        if (rightMatch.Success)
        {
            returned += rightMatch.Length;
            return returned;
        }

        return leftMatch.Success ? returned : null;
    }

    private static Nullable<int> CheckOperator(string input)
    {
        if (input.Length == 0)
        {
            return null;
        }

        return input[0] switch
        {
            '+' or '-' or '/' => 1,
            '*' => input.Length > 1 && input[1] == '*' ? 2 : 1,
            _ => null,
        };
    }

    private static (int, bool) PartialLex(string token, int index, bool wasCloseParenth, List<Lexeme> outputList)
    {
        outputList.Clear();

        if (wasCloseParenth)
        {
            if (CheckOperator(token[index..]) is int lenny)
            {
                outputList.Add(Lexeme.Operator(token[index..(index + lenny)]));
                index += lenny;
            }
        }

        if (CheckOperator(token[index..]) is int len2)
        {
            outputList.Add(Lexeme.Operator(token[index..(index + len2)]));
            index += len2;
        }

        // Bro I just started using :Format for once. I fucking hate the C# convention of
        // formatting
        if (token[index..].Length > 0)
        {
            if (token[index] == '(')
            {
                outputList.Add(Lexeme.IncPrecedence("("));
                index++;
            }
            else if (token[index] == ')')
            {
                outputList.Add(Lexeme.DecPrecedence(")"));
                return (index + 1, true);
            }

            if (CheckNumber(token[index..]) is int len)
            {
                outputList.Add(Lexeme.Number(token[index..(index + len)]));
                index += len;
            }
        }

        return (index, false);
    }

    public static IEnumerable<Lexeme> Lex(string input)
    {
        List<Lexeme> partialLexResult = new();

        foreach (string bigToken in String.Concat(input.Select((thing) => thing == '\t' ? ' ' : thing))
            .Split(" ", StringSplitOptions.RemoveEmptyEntries))
        {
            int startIndex = 0;
            bool wasCloseParenth = false;

            while (bigToken[startIndex..].Length > 0)
            {
                (var retIndex, var retCloseParenth) = PartialLex(
                    bigToken, startIndex, wasCloseParenth, partialLexResult
                );

                startIndex = retIndex;
                wasCloseParenth = retCloseParenth;

                if (partialLexResult.Count == 0)
                {
                    throw new NotImplementedException(
                        "TODO: Find a reasonable way to recover from an invalid token. It doesn't seem too hard though."
                    );
                }

                foreach (var lexeme in partialLexResult)
                {
                    yield return lexeme;
                }
            }
        }
    }
}

class Program
{
    // TODO: Clean up this function's code a little.
    private static IEnumerable<Lexeme> Desugar(IEnumerable<Lexeme> tokens)
    {
        // If we see opening or closing parenthesis, we need to splice in a * operator before/after
        // the symbol if the symbol before/after ultamitely represents a number.

        Lexeme? left = null;
        foreach (Lexeme right in tokens)
        {
            // check left parenthesis
            if (right.ID is LexemeID.IncPrecedence && left is not null && left.Value.ID is LexemeID.Number)
            {
                yield return Lexeme.Operator("*");
            }

            // check right parenthesis
            if (left is not null && left.Value.ID is LexemeID.DecPrecedence && right.ID is (LexemeID.Number or LexemeID.IncPrecedence))
            {
                yield return Lexeme.Operator("*");
            }

            yield return right;
            left = right;
        }
    }

    static void Main(string[] args)
    {
        bool printLexemes = false;
        foreach (string arg in args)
        {
            if (arg == "--print-lexemes")
            {
                printLexemes = true;
            }
        }

        Console.Error.Write("> ");
        while (Console.ReadLine() is string line)
        {
            Lexeme[] lexemes = Desugar(Lexer.Lex(line)).ToArray();
            if (printLexemes)
            {
                foreach (Lexeme lexeme in lexemes)
                {
                    Console.Error.WriteLine($"\x1b[95m{lexeme}\x1b[m");
                }
            }

            IExpression expr = Parser.Parse(lexemes);

            Console.WriteLine(expr.Evaluate());
            Console.Error.Write("> ");
        }
    }
}

