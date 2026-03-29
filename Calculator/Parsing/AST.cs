using System;
using System.Collections.Generic;
using System.Linq;
namespace Calculator;

// TODO: Implement functions.
// We should have both builtin functions and custom functions. Builtin functions are actual types we
// define, while custom functions are of a single type, that we call a method to set the inputs.
// Functions of any type should have the ID `Function`, since it shouldn't matter how the function
// is implemented.

// How will these things be constructed? We should feed it in an IEnumerable<IExpression>, and have
// the constructor enforce if the right number of args was passed in.

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

public interface IFunction : IExpression {
    static abstract IExpression Build(IEnumerable<IExpression> inputs);
}

public class Sqrt : IFunction
{
    public LexemeID ID {get;} = LexemeID.SquareRoot;
    private IExpression unsquared;

    private Sqrt(IExpression unsquared) {
        this.unsquared = unsquared;
    }

    public static IExpression Build(IEnumerable<IExpression> inputs) {
        var iter = inputs.GetEnumerator();
        if(!iter.MoveNext()) {
            throw new ArgumentException("Only exactly 1 argument is allowed here");
        }
        IExpression unsquared = iter.Current;
        if(iter.MoveNext() == true) {
            throw new ArgumentException("Only exactly 1 argument is allowed here");
        }

        return new Sqrt(unsquared);
    }

    public IEnumerable<IExpression> Children() {
        yield return unsquared;
    }

    public decimal Evaluate() => (decimal)Math.Sqrt((double)unsquared.Evaluate());
}
