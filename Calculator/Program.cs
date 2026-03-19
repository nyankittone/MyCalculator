// TODO: Add error handling in the tokenizer and parser.
// TODO: Add support for pre-defined math functions, i.e. sqrt, floor, ciel, min, max, etc.
// TODO: Add support for defining custom functions.
// TODO: Use arbitrary-precision numbers instead of the built-in `decimal` type.

using System;
using System.Collections.Generic;
using System.Linq;
namespace Calculator;

class Program
{
    // TODO: Clean up this function's code a little.
    private static IEnumerable<SequentialLexeme> Desugar(IEnumerable<Lexeme> tokens)
    {
        LexemeSpawner spawn = new();
        // If we see opening or closing parenthesis, we need to splice in a * operator before/after
        // the symbol if the symbol before/after ultamitely represents a number.

        SequentialLexeme? left = null;
        foreach (SequentialLexeme right in tokens)
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

            yield return spawn.ChangeSequence(right);
            left = right;
        }
    }

    static void Main(string[] args)
    {
        (bool printLexemes, bool printExceptions) = (false, false);

        foreach (string arg in args)
        {
            switch (arg)
            {
                case "--print-lexemes":
                    printLexemes = true;
                    break;
                case "--print-exceptions":
                    printExceptions = true;
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

            // TODO: Split this code into seperate functions to reduce indentation.
            try
            {
                IExpression? expr = Parser.Parse(lexemes, new ParserExceptionFactory(line));
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

                    Console.Error.WriteLine();
                    return true;
                });

                // TODO: Make so this check doesn't have to run each time a parser error happened
                // with exception printing turned on.
                if (printExceptions)
                {
                    Console.Error.WriteLine(".NET error:\r\n{0}", ae);
                }
            }

            Console.Error.Write("> ");
        }
    }
}

