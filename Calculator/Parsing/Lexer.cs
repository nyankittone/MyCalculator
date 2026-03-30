// TODO: Comment this file.

using System;
using System.Collections.Generic;
using System.Linq;
using RE = System.Text.RegularExpressions;
namespace Calculator;

// Used for tagging lexemes with info on what exactly the lexeme is supposed to be. Also used for
// tagging nodes in an IExpression.
// In a future parser and lexer, maybe having seperate enums for these two tasks would be a good
// idea, because there have been times where the IDs here don't make sense in one place or another.
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
    Constant,
    Variable,
    BuiltinFunc,
    CustomFunc,
    Invalid,
}

// Class that adds extensions specifically for the LexemeID and ILexeme type.
public static class LexemeIDExtensions
{
    public static bool IsOperator(this LexemeID id) => id switch
    {
        LexemeID.Add or LexemeID.Subtract or LexemeID.Multiply
            or LexemeID.Divide or LexemeID.Exponent => true,
        _ => false,
    };

    public static bool IsOperator(this ILexeme lexeme) => lexeme.ID.IsOperator();
    public static bool Equals(this ILexeme x, ILexeme y) => x.ID == y.ID && x.Token == y.Token;
}

// Core type for Lexemes. It simply just has a token and a LexemeID associated with it. In practice,
// the SequentialLexeme type is usually used in things that use this type, since we need a little
// more info than what this interface alone exposes.
public interface ILexeme
{
    LexemeID ID { get; }
    string Token { get; }
}

// Minimal implementation of ILexeme for testing purposes. Actual code in the application will
// almost always use a SequentialLexeme.
public readonly record struct Lexeme(LexemeID ID, string Token) : ILexeme
{
    public override string ToString() => $"{ID}(\"{Token}\")";
}

// Main implementation of ILexeme used throughout the program. It's composed of a Lexeme, but adds
// some additional fields for storing the lexeme's index and sequence number in the context of a
// string that the tokens were extracted from.
public struct SequentialLexeme : ILexeme
{
    private Lexeme inside;
    public LexemeID ID { get => inside.ID; }
    public string Token { get => inside.Token; }

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

// This class is used to spawn SequentialLexemes in a matter where each new one has a sequence
// number 1 point higher than the last spawned SequentialLexeme. This is to aid in actually creating
// these lexemes in sequence.
public class LexemeSpawner
{
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

    // TODO: allow "^" to be used for exponents too.
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

    public SequentialLexeme Constant(string token, int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.Constant, token), index, counter++);
    public SequentialLexeme Variable(string token, int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.Variable, token), index, counter++);
    public SequentialLexeme Builtin(string token, int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.BuiltinFunc, token), index, counter++);
    public SequentialLexeme CustomFunc(string token, int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.CustomFunc, token), index, counter++);
    public SequentialLexeme Invalid(string token, int? index) =>
        new SequentialLexeme(new Lexeme(LexemeID.Invalid, token), index, counter++);

    // TODO: Consider removing a method like this in exchange for making the Sequence field mutable.
    public SequentialLexeme ChangeSequence(in ILexeme based) => new SequentialLexeme(
        new Lexeme(based.ID, based.Token),
        based is SequentialLexeme bruh ? bruh.Index : null, // performance?
        counter++
    );
}

// Static class containing functionality for the lexer.
public static class Lexer
{
    // Function that returns an iterator over each non-whitespace part of the string, using indices
    // to refer to subsections of the string so we don't lose that information while iterating with
    // it.
    private static IEnumerable<(int, int)> SplitWhitespace(string input)
    {
        RE.MatchCollection matches = RE.Regex.Matches(input, @"[^\s]+");
        return matches.Select((match) => (match.Index, match.Index + match.Length));
    }

    // Function that checks if the beginning of a string slice passed into this represents a number.
    // If yes, it returns the length of the valid section. Else, it returns null.
    private static int? CheckNumber(string input)
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

    // Function that checks if the beginning of a string slice passed in is a valid operator.
    // Returns the length of the operator in the slice if successful, and null otherwise.
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

    // Internal data structure used for returning from PartialLex().
    private struct PartialLexResult(int index, bool wasCloseParenth)
    {
        public int Index { get; } = index; // Where the PartialLex left off inside our token
        public bool WasCloseParenth { get; } = wasCloseParenth; // Whether or not the last token
                                                                // looked at was a closing parenthesis
    }

    // Function that lexes a couple tokens inside the main string passed.
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

    // This function tries to hand back a lexeme representing something that is invalid or a known
    // symbol, if we get to the end of doing a PartialLex without actually adding any more lexemes.
    private static SequentialLexeme? LexSymbol(string token, int index, in int bigIndex, ref LexemeSpawner spawn)
    {
        const string letters = @"[^0-9\(\)\+\-\*\/]";
        const string matchInvalid = @"^" + letters + @"*";
        const string matchSymbol = @"^" + letters + @"[^\(\)\+\-\*\/]*";

        var match = RE.Regex.Match(token[index..], matchSymbol);
        if(!match.Success) {
            return null;
        }

        switch(SymbolFinder.Singleton.GetSymbolType(match.Value)) {
            case SymbolType.Constant:
                return spawn.Constant(match.Value, index + bigIndex);
            case SymbolType.Variable:
                return spawn.Variable(match.Value, index + bigIndex);
            case SymbolType.BuiltinFunction:
                return spawn.Builtin(match.Value, index + bigIndex);
            case SymbolType.CustomFunction:
                return spawn.CustomFunc(match.Value, index + bigIndex);
        }

        match = RE.Regex.Match(token[index..], matchInvalid); // This may be like
                                                              // slightly slow?
        if(!match.Success) {
            return null;
        }

        return spawn.Invalid(match.Value, index + bigIndex);
    }

    // This function takes an input string, and squirts out a series of lexemes for it.
    public static IEnumerable<SequentialLexeme> Lex(string input)
    {
        LexemeSpawner spawn = new();
        List<SequentialLexeme> partialLexResult = new();

        // Splitting the whole stream into big tokens, then iterating on those to fetch little
        // tokens.
        // It's called "bigIndex" because it's the index to the "big token", which is a token for a
        // single non-whitespace region of the whole string.
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

                // If we have found no new tokens, try lexing a symbol instead
                if (partialLexResult.Count == 0)
                {
                    // Recover from an invalid token, by scanning forward until encountering a
                    // character for something valid.
                    if (LexSymbol(bigToken, startIndex, bigIndex, ref spawn) is SequentialLexeme lexeme)
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

