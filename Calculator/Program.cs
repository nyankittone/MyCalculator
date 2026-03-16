// TODO: Add error handling in the tokenizer and parser.
// TODO: Add support for pre-defined math functions, i.e. sqrt, floor, ciel, min, max, etc.
// TODO: Add support for defining custom functions.
// TODO: Use arbitrary-precision numbers instead of the built-in `decimal` type.

using System;
using System.Collections.Generic;
using System.Linq;
using RE = System.Text.RegularExpressions;
namespace Calculator;

// We're going to make the parser also take the role of the lexer, for convenience on my end bc I
// don't feel like being smart right now.
public interface IExpression
{
    public LexemeID ID { get; }
    public decimal Evaluate();
    public IEnumerable<IExpression> Children();
}

public class Number : IExpression
{
    public LexemeID ID { get; } = LexemeID.Number;
    private decimal number;
    public Number(string token)
    {
        number = Decimal.Parse(token); // TODO: Think about error handling here.
    }

    public decimal Evaluate() => number;
    public IEnumerable<IExpression> Children()
    {
        yield break;
    }
}

public abstract class Operator : IExpression
{
    public abstract LexemeID ID { get; }
    protected IExpression left;
    protected IExpression right;

    public Operator(IExpression left, IExpression right)
    {
        this.left = left;
        this.right = right;
    }

    public abstract decimal Evaluate();
    public IEnumerable<IExpression> Children()
    {
        yield return left;
        yield return right;
    }
}

// I reeeeeally wish I didn't have to explicitly mention the constructor in every derived class.
// That's a little annoying.
public class Add : Operator
{
    public override LexemeID ID { get; } = LexemeID.Add;
    public Add(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() + right.Evaluate();
}

public class Subtract : Operator
{
    public override LexemeID ID { get; } = LexemeID.Subtract;
    public Subtract(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() - right.Evaluate();
}

public class Multiply : Operator
{
    public override LexemeID ID { get; } = LexemeID.Multiply;
    public Multiply(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() * right.Evaluate();
}

public class Divide : Operator
{
    public override LexemeID ID { get; } = LexemeID.Divide;
    public Divide(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() / right.Evaluate();
}

public class Exponent : Operator
{
    public override LexemeID ID { get; } = LexemeID.Exponent;
    public Exponent(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => (decimal)Math.Pow((double)left.Evaluate(), (double)right.Evaluate());
}

public class Sqrt : IExpression
{
    public LexemeID ID { get; } = LexemeID.SquareRoot;
    private IExpression unsquared;
    public Sqrt(IExpression expr)
    {
        unsquared = expr;
    }

    // grrrrrr I hate these casts
    public decimal Evaluate() => (decimal)Math.Sqrt((double)unsquared.Evaluate());
    public IEnumerable<IExpression> Children()
    {
        yield return unsquared;
    }
}


class Program
{
    // TODO: Clean up this function's code a little.
    private static IEnumerable<Lexeme> Desugar(IEnumerable<Lexeme> tokens)
    {
        LexemeSpawner spawn = new();
        // If we see opening or closing parenthesis, we need to splice in a * operator before/after
        // the symbol if the symbol before/after ultamitely represents a number.

        Lexeme? left = null;
        foreach (Lexeme right in tokens)
        {
            // check left parenthesis
            if (right.ID is LexemeID.IncPrecedence && left is not null && left.Value.ID is LexemeID.Number)
            {
                yield return spawn.Operator("*", null);
            }

            // check right parenthesis
            if (left is not null && left.Value.ID is LexemeID.DecPrecedence && right.ID is (LexemeID.Number or LexemeID.IncPrecedence))
            {
                yield return spawn.Operator("*", null);
            }

            yield return spawn.FromLexeme(right);
            left = right;
        }
    }

    static void Main(string[] args)
    {
        Calculator.YourMom mom = new();
        mom.PrintStatus();

        bool printLexemes = false;
        foreach (string arg in args)
        {
            if (arg == "--print-lexemes")
            {
                printLexemes = true;
                break;
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

            try
            {
                IExpression expr = Parser.Parse(lexemes, new ParserExceptionFactory(line));
                if (expr is IExpression _)
                {
                    Console.WriteLine(expr.Evaluate());
                }
            }
            catch (AggregateException ae)
            {
                if (ae.InnerExceptions.Count == 1)
                {
                    Console.Error.Write("\x1b[1;91merror:\x1b[m ");
                }
                else
                {
                    Console.Error.WriteLine("\x1b[1;91mmultiple errors occured:\x1b[m");
                }

                ae.Flatten().Handle((error) =>
                {
                    if (error is ParserException parserError)
                    {
                        Console.Error.WriteLine(parserError.Message);

                        Console.Error.WriteLine(
                            "  \x1b[1m{0}\x1b[m\r\n  {1}\x1b[1;91m^{2}\x1b[m",
                            line,
                            new string(' ', parserError.Index),
                            new string('~', parserError.Length - 1)
                        );
                    }
                    else if (error is BasicParserException _)
                    {
                        Console.Error.WriteLine(error.Message);
                    }

                    Console.WriteLine();
                    return true;
                });
            }

            Console.Error.Write("> ");
        }
    }
}

