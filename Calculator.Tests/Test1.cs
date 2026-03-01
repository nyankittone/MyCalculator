namespace Calculator.Tests;

[TestClass]
public sealed class LexerTests
{
    private Lexeme Add = Lexeme.Operator("+");
    private Lexeme Sub = Lexeme.Operator("-");
    private Lexeme Mult = Lexeme.Operator("*");
    private Lexeme Div = Lexeme.Operator("/");
    private Lexeme Exp = Lexeme.Operator("**");

    private void AssertArraysEqual<T>(T[] result, T[] expected) {
        Assert.HasCount(expected.Length, result);
        for(int i = 0; i < result.Length; i++) {
            Assert.AreEqual(result[i], expected[i]);
        }
    }

    private void DidItTwoPlusTwo(Lexeme[] testOn) {
        AssertArraysEqual(testOn, [Lexeme.Number("2"), Add, Lexeme.Number("2")]);
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
        AssertArraysEqual(result, [Lexeme.Number("2"), Add, Lexeme.Number("+2")]);
    }

    [TestMethod]
    public void BrokenTwoPlusPositiveTwo() {
        var result = Lexer.Lex("2 ++ 2").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("2"), Add, Add, Lexeme.Number("2")]);
    }

    [TestMethod]
    public void SubtractThing() {
        var result = Lexer.Lex("69-420").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("69"), Sub, Lexeme.Number("420")]);
    }

    [TestMethod]
    public void DoubleSubtractThing() {
        var result = Lexer.Lex("69--420").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("69"), Sub, Lexeme.Number("-420")]);
    }

    [TestMethod]
    public void BrokenDoubleSubtractThing() {
        var result = Lexer.Lex("69 -- 420").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("69"), Sub, Sub, Lexeme.Number("420")]);
    }

    [TestMethod]
    public void OperatorSpam() {
        var result = Lexer.Lex("+*-///---+-+-/*+**-/+").ToArray();
        Lexeme[] expected = {
            Add,
            Mult,
            Sub,
            Div,
            Div,
            Div,
            Sub,
            Sub,
            Sub,
            Add,
            Sub,
            Add,
            Sub,
            Div,
            Mult,
            Add,
            Exp,
            Sub,
            Div,
            Add,
        };

        AssertArraysEqual(result, expected);
    }
}
