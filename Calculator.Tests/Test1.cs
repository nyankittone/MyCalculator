namespace Calculator.Tests;

[TestClass]
public sealed class LexerTests
{
    private Lexeme Add = Lexeme.Operator("+");
    private Lexeme Sub = Lexeme.Operator("-");
    private Lexeme Mult = Lexeme.Operator("*");
    private Lexeme Div = Lexeme.Operator("/");
    private Lexeme Exp = Lexeme.Operator("**");

    private void DidItTwoPlusTwo(Lexeme[] testOn) {
        Assert.HasCount(3, testOn);
        Assert.AreEqual(testOn[0], Lexeme.Number("2"));
        Assert.AreEqual(testOn[1], Add);
        Assert.AreEqual(testOn[2], Lexeme.Number("2"));
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
        Assert.HasCount(3, result);
        Assert.AreEqual(result[0], Lexeme.Number("2"));
        Assert.AreEqual(result[1], Add);
        Assert.AreEqual(result[2], Lexeme.Number("+2"));
    }

    [TestMethod]
    public void BrokenTwoPlusPositiveTwo() {
        var result = Lexer.Lex("2 ++ 2").ToArray();
        Assert.HasCount(4, result);
        Assert.AreEqual(result[0], Lexeme.Number("2"));
        Assert.AreEqual(result[1], Add);
        Assert.AreEqual(result[2], Add);
        Assert.AreEqual(result[3], Lexeme.Number("2"));
    }

    [TestMethod]
    public void SubtractThing() {
        var result = Lexer.Lex("69-420").ToArray();
        Assert.HasCount(3, result);
        Assert.AreEqual(result[0], Lexeme.Number("69"));
        Assert.AreEqual(result[1], Sub);
        Assert.AreEqual(result[2], Lexeme.Number("420"));
    }

    [TestMethod]
    public void DoubleSubtractThing() {
        var result = Lexer.Lex("69--420").ToArray();
        Assert.HasCount(3, result);
        Assert.AreEqual(result[0], Lexeme.Number("69"));
        Assert.AreEqual(result[1], Sub);
        Assert.AreEqual(result[2], Lexeme.Number("-420"));
    }

    [TestMethod]
    public void BrokenDoubleSubtractThing() {
        var result = Lexer.Lex("69 -- 420").ToArray();
        Assert.HasCount(4, result);
        Assert.AreEqual(result[0], Lexeme.Number("69"));
        Assert.AreEqual(result[1], Sub);
        Assert.AreEqual(result[2], Sub);
        Assert.AreEqual(result[3], Lexeme.Number("420"));
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

        Assert.HasCount(expected.Length, result);
        // can I just Assert.AreEqual() the entire array at once?
        for(int i = 0; i < result.Length; i++) {
            Assert.AreEqual(result[i], expected[i]);
        }
    }
}
