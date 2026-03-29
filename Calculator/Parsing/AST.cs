using System;
using System.Collections.Generic;
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

public abstract class Builtin : IExpression {
    public LexemeID ID {get;} = LexemeID.BuiltinFunc;
    public abstract IEnumerable<IExpression> Children();
    public abstract decimal Evaluate();
}

public abstract class OneParamFunc(IExpression child) : Builtin {
    protected IExpression child = child;
    public override IEnumerable<IExpression> Children()
    {
        yield return child;
    }
}

public class Sqrt : OneParamFunc
{
    public Sqrt(IExpression thing) : base(thing) {}
    public override decimal Evaluate() => (decimal)Math.Sqrt((double)child.Evaluate());
}

public class Floor : OneParamFunc
{
    public Floor(IExpression thing) : base(thing) {}
    public override decimal Evaluate() => (decimal)Math.Floor((double)child.Evaluate());
}

public class Ceil : OneParamFunc
{
    public Ceil(IExpression thing) : base(thing) {}
    public override decimal Evaluate() => (decimal)Math.Ceiling((double)child.Evaluate());
}

public class Min(IEnumerable<IExpression> children) : Builtin {
    private IEnumerable<IExpression> children = children;
    public override IEnumerable<IExpression> Children() => children;
    public override decimal Evaluate() {
        var iter = children.GetEnumerator();
        iter.MoveNext();
        decimal returned = iter.Current.Evaluate();

        while(iter.MoveNext() == true) {
            returned = Math.Min(returned, iter.Current.Evaluate());
        }

        return returned;
    }
}

public class Max(IEnumerable<IExpression> children) : Builtin {
    private IEnumerable<IExpression> children = children;
    public override IEnumerable<IExpression> Children() => children;
    public override decimal Evaluate() {
        var iter = children.GetEnumerator();
        iter.MoveNext();
        decimal returned = iter.Current.Evaluate();

        while(iter.MoveNext() == true) {
            returned = Math.Max(returned, iter.Current.Evaluate());
        }

        return returned;
    }
}
