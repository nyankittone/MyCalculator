namespace Calculator.Tests;

// Handing off L's to this whole codebase
static class L {
    public static Lexeme Add = Lexeme.Operator("+");
    public static Lexeme Sub = Lexeme.Operator("-");
    public static Lexeme Mult = Lexeme.Operator("*");
    public static Lexeme Div = Lexeme.Operator("/");
    public static Lexeme Exp = Lexeme.Operator("**");
    public static Lexeme Open = Lexeme.IncPrecedence("(");
    public static Lexeme Close = Lexeme.DecPrecedence(")");
}

[TestClass]
public sealed class ParserTests {
    // what do I even need to test here?
    // What trees are generated from certain lexeme sequences, of course!
    // I must remember to also test cases where the parsing should fail.
    // I will need to be able to examine the structure of the tree more deeply. This means either
    // changing my IExpression data structure to make it so I can perform that examination, or abuse
    // reflection. This is providing a good excuse for me to learn reflection, but for now I will
    // do it the other way.
    [TestMethod]
    public void SixSeven() {
        Lexeme[] input = {Lexeme.Number("67")};
        Assert.AreEqual(new Number("67").Evaluate(), Parser.Parse(input).Evaluate());
    }
}

[TestClass]
public sealed class LexerTests
{
    private void AssertArraysEqual<T>(T[] result, T[] expected) {
        Assert.HasCount(expected.Length, result);
        for(int i = 0; i < result.Length; i++) {
            Assert.AreEqual(result[i], expected[i]);
        }
    }

    private void DidItTwoPlusTwo(Lexeme[] testOn) {
        AssertArraysEqual(testOn, [Lexeme.Number("2"), L.Add, Lexeme.Number("2")]);
    }

    [TestMethod]
    public void TwoPlusTwo()
    {
        var result = Lexer.Lex("2 + 2").ToArray();
        DidItTwoPlusTwo(result);
    }

    [TestMethod]
    public void TwoPlusTwoCompressed() {
        var result = Lexer.Lex("2+2").ToArray();
        DidItTwoPlusTwo(result);
    }

    [TestMethod]
    public void TwoPlusTwoWhitespace() {
        var result = Lexer.Lex("    2     + 2       ").ToArray();
        DidItTwoPlusTwo(result);
    }

    [TestMethod]
    public void TwoPlusTwoTabs() {
        var result = Lexer.Lex("\t2\t+\t2\t").ToArray();
        DidItTwoPlusTwo(result);
    }

    [TestMethod]
    public void TwoPlusPositiveTwo() {
        var result = Lexer.Lex("2++2").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("2"), L.Add, Lexeme.Number("+2")]);
    }

    [TestMethod]
    public void BrokenTwoPlusPositiveTwo() {
        var result = Lexer.Lex("2 ++ 2").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("2"), L.Add, L.Add, Lexeme.Number("2")]);
    }

    [TestMethod]
    public void SubtractThing() {
        var result = Lexer.Lex("69-420").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("69"), L.Sub, Lexeme.Number("420")]);
    }

    [TestMethod]
    public void DoubleSubtractThing() {
        var result = Lexer.Lex("69--420").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("69"), L.Sub, Lexeme.Number("-420")]);
    }

    [TestMethod]
    public void BrokenDoubleSubtractThing() {
        var result = Lexer.Lex("69 -- 420").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("69"), L.Sub, L.Sub, Lexeme.Number("420")]);
    }

    [TestMethod]
    public void OperatorSpam() {
        var result = Lexer.Lex("+*-///---+-+-/*+**-/+").ToArray();
        Lexeme[] expected = {
            L.Add,
            L.Mult,
            L.Sub,
            L.Div,
            L.Div,
            L.Div,
            L.Sub,
            L.Sub,
            L.Sub,
            L.Add,
            L.Sub,
            L.Add,
            L.Sub,
            L.Div,
            L.Mult,
            L.Add,
            L.Exp,
            L.Sub,
            L.Div,
            L.Add,
        };

        AssertArraysEqual(result, expected);
    }

    [TestMethod]
    public void LongBar() {
        var result = Lexer.Lex("8------------3").ToArray();
        AssertArraysEqual(result, [
            Lexeme.Number("8"),
            L.Sub,
            L.Sub,
            L.Sub,
            L.Sub,
            L.Sub,
            L.Sub,
            L.Sub,
            L.Sub,
            L.Sub,
            L.Sub,
            L.Sub,
            Lexeme.Number("-3"),
        ]);
    }

    [TestMethod]
    public void Stars() {
        var result = Lexer.Lex("***********").ToArray();
        AssertArraysEqual(result, [L.Exp, L.Exp, L.Exp, L.Exp, L.Exp, L.Mult]);
    }

    [TestMethod]
    public void ParenthesisSpam() {
        var result = Lexer.Lex("89(((-7)(+6))))(-4-4(()+67()-69").ToArray();
        AssertArraysEqual(result, [
            Lexeme.Number("89"),
            L.Open,
            L.Open,
            L.Open,
            Lexeme.Number("-7"),
            L.Close,
            L.Open,
            Lexeme.Number("+6"),
            L.Close,
            L.Close,
            L.Close,
            L.Close,
            L.Open,
            Lexeme.Number("-4"),
            L.Sub,
            Lexeme.Number("4"),
            L.Open,
            L.Open,
            L.Close,
            L.Add,
            Lexeme.Number("67"),
            L.Open,
            L.Close,
            L.Sub,
            Lexeme.Number("69"),
        ]);
    }
}
