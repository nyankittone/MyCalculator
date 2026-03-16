using System;
using System.Collections.Generic;
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

