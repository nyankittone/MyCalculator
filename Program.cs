// TODO: Add handling of parenthesis in the way that I want.
// TODO: Add error handling in the tokenizer and parser.
// TODO: Add exponent support with "**" as the operator.
// TODO: Add support for pre-defined math functions, i.e. sqrt, floor, ciel, min, max, etc.
// TODO: Add support for defining custom functions.

using System;
using System.Collections.Generic;
using System.Linq;
// using System.Text;
using RE = System.Text.RegularExpressions;
namespace Calculator;

enum LexemeID {
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

struct Lexeme {
    public LexemeID ID {get;}
    public string? token {get;}

    private Lexeme(LexemeID ID, string? token) {
        this.ID = ID;
        this.token = token;
    }

    public override string ToString() {
        return $"{ID}({token})";
    }

    public static Lexeme Number(string token) => new Lexeme(LexemeID.Number, token);
    public static Lexeme Operator(string token) => new Lexeme(LexemeID.Operator, token);
    public static Lexeme DecPrecedence(string token) => new Lexeme(LexemeID.DecPrecedence, token);
}

// We're going to make the parser also take the role of the lexer, for convenience on my end bc I
// don't feel like being smart right now.
interface IExpression {
    public decimal Evaluate();
}

class Number : IExpression {
    private decimal number;
    public Number(string token) {
        number = Decimal.Parse(token); // TODO: Think about error handling here.
    }

    public decimal Evaluate() => number;
}

abstract class Operator : IExpression {
    protected IExpression left;
    protected IExpression right;

    public Operator(IExpression left, IExpression right) {
        this.left = left;
        this.right = right;
    }

    public abstract decimal Evaluate();
}

// I reeeeeally wish I didn't have to explicitly mention the constructor in every derived class.
// That's a little annoying.
class Add : Operator {
    public Add(IExpression left, IExpression right) : base(left, right) {}
    public override decimal Evaluate() => left.Evaluate() + right.Evaluate();
}

class Subtract : Operator {
    public Subtract(IExpression left, IExpression right) : base(left, right) {}
    public override decimal Evaluate() => left.Evaluate() - right.Evaluate();
}

class Multiply : Operator {
    public Multiply(IExpression left, IExpression right) : base(left, right) {}
    public override decimal Evaluate() => left.Evaluate() * right.Evaluate();
}

class Divide : Operator {
    public Divide(IExpression left, IExpression right) : base(left, right) {}
    public override decimal Evaluate() => left.Evaluate() / right.Evaluate();
}

class Exponent : Operator {
    public Exponent(IExpression left, IExpression right) : base(left, right) {}
    public override decimal Evaluate() => (decimal)Math.Pow((double)left.Evaluate(), (double)right.Evaluate());
}

class Sqrt : IExpression {
    private IExpression unsquared;
    public Sqrt(IExpression expr) {
        unsquared = expr;
    }

    // grrrrrr I hate these casts
    public decimal Evaluate() => (decimal)Math.Sqrt((double)unsquared.Evaluate());
}

class Program {
    private static IEnumerable<Lexeme> Lex(string input) {
        Nullable<int> CheckNumber(string input) {
            if(input.Length == 0) {
                return null;
            }

            int returned = 0;
            if(input[0] == '+' || input[0] == '-') {
                returned++;
            }

            RE.Match leftMatch = RE.Regex.Match(input[returned..], @"^\d+");
            if(leftMatch.Success) {
                returned += leftMatch.Length;
            }

            if(input[returned..].Length == 0 || input[returned] != '.') {
                return leftMatch.Success ? returned : null;
            }

            returned++;

            RE.Match rightMatch = RE.Regex.Match(input[returned..], @"^\d+");
            if(rightMatch.Success) {
                returned += rightMatch.Length;
                return returned;
            }

            return leftMatch.Success ? returned : null;
        }

        Nullable<int> CheckOperator(string input) {
            if(input.Length == 0) {
                return null;
            }

            return input[0] switch {
                '+' or '-' or '/' => 1,
                '*' => input.Length > 1 && input[1] == '*' ? 2 : 1,
                _ => null,
            };
        }

        foreach(string bigToken in String.Concat(input.Select((thing) => thing == '\t' ? ' ' : thing))
            .Split(" ", StringSplitOptions.RemoveEmptyEntries))
        {
            int startIndex = 0;

            // TODO: Consider removing this while shuffling around some stuff in the while loop
            // below. I think this part is redundant.
            if(CheckNumber(bigToken) is int length) {
                yield return Lexeme.Number(bigToken[..length]);
                startIndex = length;
            }

            while(bigToken[startIndex..].Length > 0) {
                int oldStart = startIndex;

                if(CheckOperator(bigToken[startIndex..]) is int len2) {
                    yield return Lexeme.Operator(bigToken[startIndex..(startIndex+len2)]);
                    startIndex += len2;
                }

                if(CheckNumber(bigToken[startIndex..]) is int len) {
                    yield return Lexeme.Number(bigToken[startIndex..(startIndex+len)]);
                    startIndex += len;
                }

                if(oldStart == startIndex) {
                    throw new NotImplementedException (
                        "TODO: Find a reasonable way to recover from an invalid token."
                    );
                }
            }
        }
    }

    // TODO: This parser ignores the lexer's identifiers of what kind of lexeme each element is.
    // I'll want to rewrite this to make it actually use that information.
    // TODO: Clean up parser code...
    private static IExpression BuildTree(IEnumerable<Lexeme> tokens) {
        IExpression MergeMid(IExpression? left, IExpression right, string? op) {
            if(left is IExpression theLeft) {
                left = op switch {
                    "*" => new Multiply(theLeft, right),
                    "/" => new Divide(theLeft, right),
                    _ => throw new NotImplementedException("Not multiply or divide here"),
                };
            } else {
                left = right; // Idk if this is right lol
            }
            return left;
        }

        // A valid expression should have tokens representing numbers for the first and last token.
        // Middle tokens should alternate between an operator and a number.
        (IExpression? left, IExpression? mid, IExpression? right) = (null, null, null);
        string? oldAddOperator = null;
        string? oldMultOperator = null;

        using(var enumerator = tokens.GetEnumerator()) {
            if(!enumerator.MoveNext()) {
                throw new NotImplementedException("TODO: Implement error for no expression passed");
            }
            right = new Number(enumerator.Current.token); // TODO: Add exception handling here

            // read two tokens at a time, first one should be an operator, second should be a number
            while(enumerator.MoveNext()) {
                string operatorToken = enumerator.Current.token;
                if(!enumerator.MoveNext()) {
                    throw new NotImplementedException("TODO: Implement unbalanced expression error");
                }

                string operand = enumerator.Current.token;

                // now what???
                // We need to have two maintained trees: one for addition/subtraction, and
                // a lower one for multiplication/division...
                switch(operatorToken) {
                    case "**":
                        right = new Exponent(right, new Number(operand));
                        break;
                    case "*":
                    case "/":
                        mid = MergeMid(mid, right, oldMultOperator);
                        oldMultOperator = operatorToken;
                        right = new Number(operand);
                        break;
                    case "+":
                    case "-":
                        mid = MergeMid(mid, right, oldMultOperator);
                        right = new Number(operand);
                        oldMultOperator = null;

                        if(left is IExpression theLeft) {
                            if(oldAddOperator == "+") {
                                left = new Add(theLeft, mid);
                            } else if(oldAddOperator == "-") {
                                left = new Subtract(theLeft, mid);
                            } else {
                                throw new NotImplementedException("wtf is this operator bruh");
                            }
                        } else {
                            left = mid;
                        }

                        oldAddOperator = operatorToken;
                        // mid = new Number(operand);
                        mid = null;
                        break;
                    default:
                        throw new NotImplementedException("brooooooo wtf is this operator LMAOOO");
                }
            }
        }

        mid = MergeMid(mid, right, oldMultOperator);
        return (left, mid, oldAddOperator) switch {
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

    private static decimal Resolve(string input) {
        // My expression resolver should support:
        // addition, subtraction, multiplication, division, and exponents
        // predefined functions to compute
        // order of operations
        // reasonable errors

        // Create a tokenizer first
        return 42;
    }

    static void Main(string[] args) {
        bool printLexemes = false;
        foreach(string arg in args) {
            if(arg == "--print-lexemes") {
                printLexemes = true;
            }
        }

        Console.Error.Write("> ");
        while(Console.ReadLine() is string line) {
            IExpression expr = BuildTree(Lex(line).Select (
                printLexemes ? (Func<Lexeme, Lexeme>)((lexeme) => { // TODO: Print colors better...
                    Console.Error.WriteLine($"\x1b[95m{lexeme}\x1b[m");
                    return lexeme;
                }) : (l) => l
            ));

            Console.WriteLine(expr.Evaluate());
            Console.Error.Write("> ");
        }
    }
}

