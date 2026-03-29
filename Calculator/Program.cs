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
    private static IEnumerable<SequentialLexeme> Desugar(IEnumerable<SequentialLexeme> tokens)
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

    private static Func<Exception, bool> HandleError(string line) => (error) => {
        if (error is ParserException parserError)
        {
            Console.Error.WriteLine(parserError.Message);
            if(parserError.Index is int idx && parserError.Length is int len) {
                Console.Error.WriteLine(
                    "  \x1b[1m{0}\x1b[m\r\n  {1}\x1b[1;91m^{2}\x1b[m",
                    line,
                    new string(' ', idx),
                    new string('~', len - 1)
                );
            }
        }
        else if(error is (StackOverflowException or ArgumentException or ArithmeticException or NullReferenceException)) {
            Exception printedError = error.InnerException ?? error;
            Console.Error.WriteLine (
                $"\x1b[1;91mPROCESS ERROR:\x1b[m {error.Message}\r\n" + 
                $"\x1b[1m---------------------\x1b[m\r\n" + 
                $"{printedError}\r\n" +
                $"\x1b[1m---------------------\x1b[m\r\n"
            );
        } else {
            Console.Error.WriteLine(">>>FUCK<<<");
            return false;
        }

        Console.Error.WriteLine();
        return true;
    };

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
            SequentialLexeme[] lexemes = Desugar(Lexer.Lex(line)).ToArray();
            if (printLexemes)
            {
                foreach (SequentialLexeme lexeme in lexemes)
                {
                    Console.Error.WriteLine($"\x1b[95m{lexeme}\x1b[m");
                }
            }

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

                ae.Handle(HandleError(line));

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

