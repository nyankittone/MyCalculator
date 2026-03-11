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
    Add,
    Subtract,
    Multiply,
    Divide,
    Exponent,
    SquareRoot,
    IncPrecedence,
    DecPrecedence,
    Func,
    Invalid,
}

public struct Lexeme
{
    public LexemeID ID {get;}
    public string token {get;}
    public int? index {get;}

    private Lexeme(LexemeID ID, string token, int? index)
    {
        this.ID = ID;
        this.token = token;
        this.index = index;
    }

    public override string ToString()
    {
        return $"@{index + 1}: {ID}(\"{token}\")";
    }

    public static Lexeme Number(string token, int? index) => new Lexeme(LexemeID.Number, token, index);
    public static Lexeme Add(int? index) => new Lexeme(LexemeID.Add, "+", index);
    public static Lexeme Subtract(int? index) => new Lexeme(LexemeID.Subtract, "-", index);
    public static Lexeme Multiply(int? index) => new Lexeme(LexemeID.Multiply, "*", index);
    public static Lexeme Divide(int? index) => new Lexeme(LexemeID.Divide, "/", index);
    public static Lexeme Exponent(int? index) => new Lexeme(LexemeID.Exponent, "**", index);

    public static Lexeme Operator(string token, int? index) {
        LexemeID id = token switch {
            "+" => LexemeID.Add,
            "-" => LexemeID.Subtract,
            "*" => LexemeID.Multiply,
            "/" => LexemeID.Divide,
            "**" => LexemeID.Exponent,
            _ => throw new ArgumentException($"Invalid operator token {token}."),
        };

        return new Lexeme(id, token, index);
    }

    public static Lexeme IncPrecedence(string token, int? index) => new Lexeme(LexemeID.IncPrecedence, token, index);
    public static Lexeme DecPrecedence(string token,int? index) => new Lexeme(LexemeID.DecPrecedence, token, index);
    public static Lexeme Invalid(string token, int? index) => new Lexeme(LexemeID.Invalid, token, index);
}

// We're going to make the parser also take the role of the lexer, for convenience on my end bc I
// don't feel like being smart right now.
public interface IExpression
{
    public LexemeID ID {get;}
    public decimal Evaluate();
    public IEnumerable<IExpression> Children();
}

public class Number : IExpression
{
    public LexemeID ID {get;} = LexemeID.Number;
    private decimal number;
    public Number(string token)
    {
        number = Decimal.Parse(token); // TODO: Think about error handling here.
    }

    public decimal Evaluate() => number;
    public IEnumerable<IExpression> Children() {
        yield break;
    }
}

public abstract class Operator : IExpression
{
    public abstract LexemeID ID {get;}
    protected IExpression left;
    protected IExpression right;

    public Operator(IExpression left, IExpression right)
    {
        this.left = left;
        this.right = right;
    }

    public abstract decimal Evaluate();
    public IEnumerable<IExpression> Children() {
        yield return left;
        yield return right;
    }
}

// I reeeeeally wish I didn't have to explicitly mention the constructor in every derived class.
// That's a little annoying.
public class Add : Operator
{
    public override LexemeID ID {get;} = LexemeID.Add;
    public Add(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() + right.Evaluate();
}

public class Subtract : Operator
{
    public override LexemeID ID {get;} = LexemeID.Subtract;
    public Subtract(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() - right.Evaluate();
}

public class Multiply : Operator
{
    public override LexemeID ID {get;} = LexemeID.Multiply;
    public Multiply(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() * right.Evaluate();
}

public class Divide : Operator
{
    public override LexemeID ID {get;} = LexemeID.Divide;
    public Divide(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => left.Evaluate() / right.Evaluate();
}

public class Exponent : Operator
{
    public override LexemeID ID {get;} = LexemeID.Exponent;
    public Exponent(IExpression left, IExpression right) : base(left, right) { }
    public override decimal Evaluate() => (decimal)Math.Pow((double)left.Evaluate(), (double)right.Evaluate());
}

public class Sqrt : IExpression
{
    public LexemeID ID {get;} = LexemeID.SquareRoot;
    private IExpression unsquared;
    public Sqrt(IExpression expr)
    {
        unsquared = expr;
    }

    // grrrrrr I hate these casts
    public decimal Evaluate() => (decimal)Math.Sqrt((double)unsquared.Evaluate());
    public IEnumerable<IExpression> Children() {
        yield return unsquared;
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

    private static int? CheckOperator(string input)
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

    private struct PartialLexResult(int index, bool wasCloseParenth) {
        public int Index {get;} = index; // Where the PartialLex left off inside our token
        public bool WasCloseParenth {get;} = wasCloseParenth; // Whether or not the last token
                                                              // looked at was a closing parenthesis
    }

    private static PartialLexResult PartialLex(string token, int index, in int bigIndex, bool wasCloseParenth, List<Lexeme> outputList)
    {
        outputList.Clear();

        if (wasCloseParenth)
        {
            if (CheckOperator(token[index..]) is int lenny)
            {
                outputList.Add(Lexeme.Operator(token[index..(index + lenny)], index + bigIndex));
                index += lenny;
            }
        }

        if (CheckOperator(token[index..]) is int len2)
        {
            outputList.Add(Lexeme.Operator(token[index..(index + len2)], index + bigIndex));
            index += len2;
        }

        // Bro I just started using :Format for once. I fucking hate the C# convention of
        // formatting
        if (token[index..].Length > 0)
        {
            if (token[index] == '(')
            {
                outputList.Add(Lexeme.IncPrecedence("(", index + bigIndex));
                index++;
            }
            else if (token[index] == ')')
            {
                outputList.Add(Lexeme.DecPrecedence(")", index + bigIndex));
                return new PartialLexResult(index + 1, true);
            }

            if (CheckNumber(token[index..]) is int len)
            {
                outputList.Add(Lexeme.Number(token[index..(index + len)], index + bigIndex));
                index += len;
            }
        }

        return new PartialLexResult(index, false);
    }

    private static Lexeme? LexInvalid(string token, int index, in int bigIndex) {
        var match = RE.Regex.Match(token[index..], @"^[^0-9\(\)\+\-\*\/]*"); // This may be like
                                                                             // slightly slow?
        return match.Success switch {
            true => Lexeme.Invalid(match.Value, index + bigIndex),
            false => null,
        };
    }

    public static IEnumerable<Lexeme> Lex(string input)
    {
        List<Lexeme> partialLexResult = new();
        int bigIndex = 0;

        // TODO: Iterate between whitespace while preserving info about where the whitespace is and
        // how much of it is there, so we can get more accurate index numbers for each lexeme.
        foreach (string bigToken in String.Concat(input.Select((thing) => thing == '\t' ? ' ' : thing))
            .Split(" ", StringSplitOptions.RemoveEmptyEntries))
        {
            int startIndex = 0;
            bool wasCloseParenth = false;

            while (bigToken[startIndex..].Length > 0)
            {
                var result = PartialLex(
                    bigToken, startIndex, bigIndex, wasCloseParenth, partialLexResult
                );

                startIndex = result.Index;
                wasCloseParenth = result.WasCloseParenth;

                if (partialLexResult.Count == 0)
                {
                    // Recover from an invalid token, by scanning forward until encountering a
                    // character for something valid.
                    if(LexInvalid(bigToken, startIndex, bigIndex) is Lexeme lexeme) {
                        yield return lexeme;
                        startIndex += lexeme.token.Length;
                    } else {
                        throw new Exception("Couldn't match invalid characters on invalid token!!!");
                    }
                }

                foreach (var lexeme in partialLexResult)
                {
                    yield return lexeme;
                }
            }

            bigIndex += bigToken.Length + 1;
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
                yield return Lexeme.Operator("*", null);
            }

            // check right parenthesis
            if (left is not null && left.Value.ID is LexemeID.DecPrecedence && right.ID is (LexemeID.Number or LexemeID.IncPrecedence))
            {
                yield return Lexeme.Operator("*", null);
            }

            yield return right;
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

            IExpression expr = Parser.Parse(lexemes, new ParserExceptionFactory(line));

            Console.WriteLine(expr.Evaluate());
            Console.Error.Write("> ");
        }
    }
}



