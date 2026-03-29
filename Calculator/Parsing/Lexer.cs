// TODO: Comment this file.

using System;
using System.Collections.Generic;
using System.Linq;
using RE = System.Text.RegularExpressions;
namespace Calculator;

public enum LexemeID
{
    None,
    Number,
    Add,
    Subtract,
    Multiply,
    Divide,
    Exponent,
    IncPrecedence,
    DecPrecedence,
    BuiltinFunc,
    CustomFunc,
    Invalid,
}

public static class LexemeIDExtensions
{
    public static bool IsOperator(this LexemeID id) => id switch
    {
        LexemeID.Add or LexemeID.Subtract or LexemeID.Multiply
            or LexemeID.Divide or LexemeID.Exponent => true,
        _ => false,
    };

    // Totally not sketchy to have default methods as extensions... but it's the most ergonomic way
    // I can do this imo.
    public static bool IsOperator(this ILexeme lexeme) => lexeme.ID.IsOperator();
    public static bool Equals(this ILexeme x, ILexeme y) => x.ID == y.ID && x.Token == y.Token;
}

public interface ILexeme {
    LexemeID ID {get;}
    string Token {get;}
}

public readonly record struct Lexeme(LexemeID ID, string Token) : ILexeme {
    public override string ToString() => $"{ID}(\"{Token}\")"; // This might make more sense to add
                                                               // to the interface
}

// In the future, a better achitectural decision would be to make this an interface. With one data
// type having the nullable properties and the othe4r not having them. I didn't feel like doing that
// here, but yeah...
public struct SequentialLexeme : ILexeme
{
    private Lexeme inside;
    public LexemeID ID {get => inside.ID;}
    public string Token {get => inside.Token;}

    public int? Index { get; }
    public uint SeqIndex { get; }

    public SequentialLexeme(Lexeme lexeme, int? index, uint sequence)
    {
        this.inside = lexeme;
        this.Index = index;
        this.SeqIndex = sequence;
    }

    public override string ToString() => $"@{Index + 1}: {inside}";
}

public class LexemeSpawner {
    private uint counter = 0;

    public SequentialLexeme Number(string token, int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.Number, token), index, counter++);
    public SequentialLexeme Add(int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.Add, "+"), index, counter++);
    public SequentialLexeme Subtract(int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.Subtract, "-"), index, counter++);
    public SequentialLexeme Multiply(int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.Multiply, "*"), index, counter++);
    public SequentialLexeme Divide(int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.Divide, "/"), index, counter++);
    public SequentialLexeme Exponent(int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.Exponent, "**"), index, counter++);

    public SequentialLexeme Operator(string token, int? index)
    {
        LexemeID id = token switch
        {
            "+" => LexemeID.Add,
            "-" => LexemeID.Subtract,
            "*" => LexemeID.Multiply,
            "/" => LexemeID.Divide,
            "**" => LexemeID.Exponent,
            _ => throw new ArgumentException($"Invalid operator token {token}."),
        };

        return new SequentialLexeme(new Lexeme(id, token), index, counter++);
    }

    public SequentialLexeme IncPrecedence(string token, int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.IncPrecedence, token), index, counter++);
    public SequentialLexeme DecPrecedence(string token, int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.DecPrecedence, token), index, counter++);
    public SequentialLexeme Invalid(string token, int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.Invalid, token), index, counter++);

    // TODO: Consider removing a method like this in exchange for making the Sequence field mutable.
    public SequentialLexeme ChangeSequence(in ILexeme based) => new SequentialLexeme (
        new Lexeme(based.ID, based.Token),
        based is SequentialLexeme bruh ? bruh.Index : null, // performance?
        counter++
    );
}

public static class Lexer
{
    private static IEnumerable<(int, int)> SplitWhitespace(string input) {
        RE.MatchCollection matches = RE.Regex.Matches(input, @"[^\s]+");
        foreach(RE.Match match in matches) {
            yield return (match.Index, match.Index + match.Length);
        }
    }

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

    private struct PartialLexResult(int index, bool wasCloseParenth)
    {
        public int Index { get; } = index; // Where the PartialLex left off inside our token
        public bool WasCloseParenth { get; } = wasCloseParenth; // Whether or not the last token
                                                                // looked at was a closing parenthesis
    }

    private static PartialLexResult PartialLex(string token, int index, in int bigIndex, bool wasCloseParenth, List<SequentialLexeme> outputList, ref LexemeSpawner spawn)
    {
        outputList.Clear();

        if (wasCloseParenth)
        {
            if (CheckOperator(token[index..]) is int lenny)
            {
                outputList.Add(spawn.Operator(token[index..(index + lenny)], index + bigIndex));
                index += lenny;
            }
        }

        if (CheckOperator(token[index..]) is int len2)
        {
            outputList.Add(spawn.Operator(token[index..(index + len2)], index + bigIndex));
            index += len2;
        }

        // Bro I just started using :Format for once. I fucking hate the C# convention of
        // formatting
        if (token[index..].Length > 0)
        {
            if (token[index] == '(')
            {
                outputList.Add(spawn.IncPrecedence("(", index + bigIndex));
                index++;
            }
            else if (token[index] == ')')
            {
                outputList.Add(spawn.DecPrecedence(")", index + bigIndex));
                return new PartialLexResult(index + 1, true);
            }

            if (CheckNumber(token[index..]) is int len)
            {
                outputList.Add(spawn.Number(token[index..(index + len)], index + bigIndex));
                index += len;
            }
        }

        return new PartialLexResult(index, false);
    }

    private static SequentialLexeme? LexInvalid(string token, int index, in int bigIndex, ref LexemeSpawner spawn)
    {
        var match = RE.Regex.Match(token[index..], @"^[^0-9\(\)\+\-\*\/]*"); // This may be like
                                                                             // slightly slow?
        return match.Success switch
        {
            true => spawn.Invalid(match.Value, index + bigIndex),
            false => null,
        };
    }

    public static IEnumerable<SequentialLexeme> Lex(string input)
    {
        LexemeSpawner spawn = new();
        List<SequentialLexeme> partialLexResult = new();

        // TODO: Iterate between whitespace while preserving info about where the whitespace is and
        // how much of it is there, so we can get more accurate index numbers for each lexeme.
        foreach ((int bigIndex, int endIndex) in SplitWhitespace(input))
        {
            int startIndex = 0;
            string bigToken = input[bigIndex..endIndex];
            bool wasCloseParenth = false;

            while (bigToken[startIndex..].Length > 0)
            {
                var result = PartialLex(
                    bigToken, startIndex, bigIndex, wasCloseParenth, partialLexResult, ref spawn
                );

                startIndex = result.Index;
                wasCloseParenth = result.WasCloseParenth;

                if (partialLexResult.Count == 0)
                {
                    // Recover from an invalid token, by scanning forward until encountering a
                    // character for something valid.
                    if (LexInvalid(bigToken, startIndex, bigIndex, ref spawn) is SequentialLexeme lexeme)
                    {
                        yield return lexeme;
                        startIndex += lexeme.Token.Length;
                    }
                    else
                    {
                        // TODO: Find a better exception to throw here.
                        throw new Exception("Couldn't match invalid characters on invalid token!!!");
                    }
                }

                foreach (var lexeme in partialLexResult)
                {
                    yield return lexeme;
                }
            }
        }
    }
}

