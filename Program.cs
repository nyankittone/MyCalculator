// See https://aka.ms/new-console-template for more information
using System;
using System.Collections.Generic;
using System.Linq;
// using System.Text;
using RE = System.Text.RegularExpressions;
namespace Calculator;

enum LexemeID {
    Number,
    Add,
    Subtract,
    Multiply,
    Divide,
    Func,
}

// We're going to make the parser also take the role of the lexer, for convenience on my end bc I
// don't feel like being smart right now.
interface IExpression {
    public LexemeID ID {get;}
    public decimal Evaluate();
}

class Number : IExpression {
    private decimal number;
    public LexemeID ID {get;} = LexemeID.Number;

    public Number(string token) {
        number = Decimal.Parse(token); // TODO: Think about error handling here.
    }

    public decimal Evaluate() => number;
}

abstract class Operator : IExpression {
    public abstract LexemeID ID {get;}
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
    public override LexemeID ID {get;} = LexemeID.Add;
    public Add(IExpression left, IExpression right) : base(left, right) {}

    public override decimal Evaluate() => left.Evaluate() + right.Evaluate();
}

class Subtract : Operator {
    public override LexemeID ID {get;} = LexemeID.Subtract;
    public Subtract(IExpression left, IExpression right) : base(left, right) {}

    public override decimal Evaluate() => left.Evaluate() - right.Evaluate();
}

class Multiply : Operator {
    public override LexemeID ID {get;} = LexemeID.Multiply;
    public Multiply(IExpression left, IExpression right) : base(left, right) {}

    public override decimal Evaluate() => left.Evaluate() * right.Evaluate();
}

class Divide : Operator {
    public override LexemeID ID {get;} = LexemeID.Divide;
    public Divide(IExpression left, IExpression right) : base(left, right) {}

    public override decimal Evaluate() => left.Evaluate() / right.Evaluate();
}

class Sqrt : IExpression {
    public LexemeID ID {get;} = LexemeID.Func;
    private IExpression unsquared;

    public Sqrt(IExpression expr) {
        unsquared = expr;
    }

    // grrrrrr I hate these casts
    public decimal Evaluate() => (decimal)Math.Sqrt((double)unsquared.Evaluate());
}

class Program {
    private static IEnumerable<string> Tokenize(string input) {
        RE.Regex re = new(@"[\+\-\*\/]");

        foreach(string bigToken in String.Concat(input.Select((thing) => thing == '\t' ? ' ' : thing))
            .Split(" ", StringSplitOptions.RemoveEmptyEntries))
        {
            var matches = re.Matches(bigToken);
            int numberBegin = 0;

            foreach(RE.Match match in matches) {
                // get token to the left of the operator
                int numberEnd = match.Index;
                string number = bigToken[numberBegin..numberEnd];
                if(number.Length > 0) {
                    yield return number;
                }

                // get operator
                switch(match.Value) {
                    case "+":
                        yield return "+";
                        break;
                    case "-":
                        yield return "-";
                        break;
                    case "*":
                        yield return "*";
                        break;
                    case "/":
                        yield return "/";
                        break;
                    default:
                        throw new NotImplementedException("wtf is this operator bruh");
                }

                numberBegin = numberEnd + match.Length;
            }

            string finalNumber = bigToken[numberBegin..];
            if(finalNumber.Length > 0) {
                yield return finalNumber;
            }
        }
    }

    private static IExpression BuildTree(IEnumerable<string> tokens) {
        // A valid expression should have tokens representing numbers for the first and last token.
        // Middle tokens should alternate between an operator and a number.
        (IExpression? left, IExpression? right) = (null, null);
        string? oldAddOperator = null;

        using(var enumerator = tokens.GetEnumerator()) {
            if(!enumerator.MoveNext()) {
                throw new NotImplementedException("TODO: Implement error for no expression passed");
            }
            right = new Number(enumerator.Current); // TODO: Add exception handling here

            // read two tokens at a time, first one should be an operator, second should be a number
            while(enumerator.MoveNext()) {
                string operatorToken = enumerator.Current;
                if(!enumerator.MoveNext()) {
                    throw new NotImplementedException("TODO: Implement unbalanced expression error");
                }

                string operand = enumerator.Current;

                // now what???
                // We need to have two maintained trees: one for addition/subtraction, and
                // a lower one for multiplication/division...
                switch(operatorToken) {
                    case "*":
                        right = new Multiply(right, new Number(operand));
                        break;
                    case "/":
                        right = new Divide(right, new Number(operand));
                        break;
                    case "+":
                    case "-":
                        if(left is IExpression theLeft) {
                            if(oldAddOperator == "+") {
                                left = new Add(theLeft, right);
                            } else if(oldAddOperator == "-") {
                                left = new Subtract(theLeft, right);
                            } else {
                                throw new NotImplementedException("wtf is this operator bruh");
                            }
                        } else {
                            left = right; // Idk if this is right lol
                        }

                        oldAddOperator = operatorToken;
                        right = new Number(operand);
                        break;
                    default:
                        throw new NotImplementedException("brooooooo wtf is this operator LMAOOO");
                }
            }
        }

        return (left, right, oldAddOperator) switch {
            (null, null, _) => throw new Exception("Both left and right are null. How???"),
            (null, _, _) => right,
            (_, null, _) => left,
            (_, _, "+") => new Add(left, right),
            (_, _, "-") => new Subtract(left, right),
            (_, _, "*") => new Multiply(left, right),
            (_, _, "/") => new Divide(left, right),
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
        Console.Error.Write("> ");
        while(Console.ReadLine() is string line) {
            IExpression expr = BuildTree(Tokenize(line));
            Console.WriteLine(expr.Evaluate());

            Console.Error.Write("> ");
        }
    }
}

