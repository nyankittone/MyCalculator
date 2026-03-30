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

    private static (IExpression, IExpression) ValidateTwo(IEnumerable<IExpression> inputs) {
        var iter = inputs.GetEnumerator();
        if (iter.MoveNext() == false)
        {
            throw new ArgumentException("Expected 2 arguments, got none");
        }

        IExpression ret1 = iter.Current;
        if (iter.MoveNext() == false)
        {
            throw new ArgumentException("Expected 2 arguments, got 1");
        }

        IExpression ret2 = iter.Current;
        if (iter.MoveNext() == true)
        {
            throw new ArgumentException("Expected 2 arguments, got more than 2");
        }

        return (ret1, ret2);
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

    private Dictionary<string, decimal> constantStash = new Dictionary<string, decimal> {
        ["sixseven"] = 67,
        ["pi"] = 3.1415926535897932384626433832795m,
        ["e"] = 2.7182818284590452353602874713527m,
    };
    private Dictionary<string, decimal> variableStash = new();
    private Dictionary<string, Object> customStash = new();

    private Dictionary<string, Func<IEnumerable<IExpression>, IExpression>> builtinStash =
        new Dictionary<string, Func<IEnumerable<IExpression>, IExpression>>
        {
            ["sqrt"] = (range) => new Sqrt(ValidateOne(range)),
            ["floor"] = (range) => new Floor(ValidateOne(range)),
            ["ceil"] = (range) => new Ceil(ValidateOne(range)),
            ["sin"] = (range) => new Sin(ValidateOne(range)),
            ["cos"] = (range) => new Cos(ValidateOne(range)),
            ["tan"] = (range) => new Tan(ValidateOne(range)),
            ["log"] = (range) => new Log(ValidateOne(range)),

            // TODO: Fix the lexer so we can interpret symbols with numbers. Either that, or rename
            // these so that they're callable.
            ["log10"] = (range) => new Log10(ValidateOne(range)),
            ["log2"] = (range) => new Log2(ValidateOne(range)),

            ["randRange"] = (range) => {
                (var first, var second) = ValidateTwo(range);
                return new RandRange(first, second);
            },

            ["min"] = (range) => new Min(ValidateNotZero(range)),
            ["max"] = (range) => new Max(ValidateNotZero(range)),
            ["rand"] = (range) =>
            {
                ValidateNone(range);
                return new Rand();
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

    public decimal GetFalliableNumber(string symbol)
    {
        decimal returned = 0;

        if (constantStash.TryGetValue(symbol, out returned) || variableStash.TryGetValue(symbol, out returned))
        {
            return returned;
        }

        throw new KeyNotFoundException($"Number of symbol {symbol} doesn't exist");
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

