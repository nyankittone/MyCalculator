using System;
using System.Collections.Generic;
namespace Calculator;

// We need to make a system that will help us do a few things:
// * Look up a symbol and have it return if it's a variable, or function.
// * A way to get the value of a specified variable.
// * A way to instantiate a function from its name.
// Should I have a system for keeping a reference from the call to check what a symbol is for? Idk.

public enum SymbolType
{
    None,
    Constant,
    BuiltinFunction,
    Variable,
    CustomFunction,
}

// Fuck my stupid kitty life, this will be so fucking inefficient :sob:
public struct SymbolFinder()
{
    private static IExpression ValidateOne(IEnumerable<IExpression> inputs)
    {
        var iter = inputs.GetEnumerator();
        if (iter.MoveNext() == false)
        {
            throw new ArgumentException("Expected 1 argument, got none");
        }

        IExpression returned = iter.Current;
        if (iter.MoveNext() == true)
        {
            throw new ArgumentException("Expected 1 argument, got more than 1");
        }

        return returned;
    }

    private static IEnumerable<IExpression> ValidateNotZero(IEnumerable<IExpression> inputs)
    {
        var iter = inputs.GetEnumerator();
        if (iter.MoveNext() == false)
        {
            throw new ArgumentException("Expected any number of arguments, got none");
        }

        return inputs;
    }

    private static void ValidateNone(IEnumerable<IExpression> inputs)
    {
        var iter = inputs.GetEnumerator();
        if (iter.MoveNext() == true)
        {
            throw new ArgumentException("Expected no arguments, got at least 1");
        }
    }

    // evil singleton,,,,
    public static SymbolFinder Singleton = new();

    private Dictionary<string, decimal> constantStash = new Dictionary<string, decimal> { ["sixseven"] = 67 };
    private Dictionary<string, decimal> variableStash = new();
    private Dictionary<string, Object> customStash = new();

    private Dictionary<string, Func<IEnumerable<IExpression>, IExpression>> builtinStash =
        new Dictionary<string, Func<IEnumerable<IExpression>, IExpression>>
        {
            ["sqrt"] = (range) => new Sqrt(ValidateOne(range)),
            ["floor"] = (range) => new Floor(ValidateOne(range)),
            ["ceil"] = (range) => new Ceil(ValidateOne(range)),
            ["min"] = (range) => new Min(ValidateNotZero(range)),
            ["max"] = (range) => new Max(ValidateNotZero(range)),
            ["rand"] = (range) =>
            {
                ValidateNone(range);
                return new Number("42");
            },
        };

    public SymbolType GetSymbolType(string symbol)
    {
        if (constantStash.ContainsKey(symbol))
        {
            return SymbolType.Constant;
        }
        else if (builtinStash.ContainsKey(symbol))
        {
            return SymbolType.BuiltinFunction;
        }
        else if (variableStash.ContainsKey(symbol))
        {
            return SymbolType.Variable;
        }
        else if (customStash.ContainsKey(symbol))
        {
            return SymbolType.CustomFunction;
        }
        else
        {
            return SymbolType.None;
        }
    }

    public decimal? GetNumber(string symbol)
    {
        decimal returned = 0;

        if (constantStash.TryGetValue(symbol, out returned) || variableStash.TryGetValue(symbol, out returned))
        {
            return returned;
        }

        return null;
    }

    // Returning `true` means that the variable was set successfully.
    public bool SetNumber(string symbol, decimal value)
    {
        if (constantStash.ContainsKey(symbol) || builtinStash.ContainsKey(symbol))
        {
            return false;
        }

        customStash.Remove(symbol);
        variableStash[symbol] = value;
        return true;
    }

    public IExpression GetExprFromFunc(string symbol, IEnumerable<IExpression> inputs)
    {
        {
            Func<IEnumerable<IExpression>, IExpression>? fn = null;
            if (builtinStash.TryGetValue(symbol, out fn))
            {
                return fn(inputs);
            }
        }

        throw new NotImplementedException("TODO: Implement custom function calling");
    }

    // TODO: Figure out a way to load custom functions into this data type.
}

