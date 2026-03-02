namespace Calculator.Tests;

// Handing off L's to this whole codebase
static class L
{
    public static Lexeme Add = Lexeme.Operator("+");
    public static Lexeme Sub = Lexeme.Operator("-");
    public static Lexeme Mult = Lexeme.Operator("*");
    public static Lexeme Div = Lexeme.Operator("/");
    public static Lexeme Exp = Lexeme.Operator("**");
    public static Lexeme Open = Lexeme.IncPrecedence("(");
    public static Lexeme Close = Lexeme.DecPrecedence(")");
}

[TestClass]
public sealed class ParserTests
{
    private void AssertSimpleTree (
        IExpression expr, decimal expectedLeft, LexemeID expectedOperator, decimal expectedRight
    ) {
        Assert.AreEqual(expectedOperator, expr.ID);
        IExpression[] children = expr.Children().ToArray();
        Assert.HasCount(2, children);

        Assert.AreEqual(LexemeID.Number, children[0].ID);
        Assert.AreEqual(expectedLeft, children[0].Evaluate());
        Assert.AreEqual(LexemeID.Number, children[1].ID);
        Assert.AreEqual(expectedRight, children[1].Evaluate());
    }

    // We are getting the internal details of each tree with a few methods and properties defined 
    // on IExpression for getting the lexeme ID and the direct children of the node. This works for
    // our use case of checking the structure of each AST popped out of Parser.Parse(), but it's also
    // fairly limited with its abilities. Reflection is something I will need to look at in the
    // future; it looks like a really powerful way to examine the resulting tree without having to
    // pollute the tree node type with a bunch of crap just for the sake of testing.
    [TestMethod]
    public void SixSeven()
    {
        Lexeme[] input = { Lexeme.Number("67") };
        IExpression result = Parser.Parse(input);
        Assert.AreEqual(LexemeID.Number, result.ID);
        Assert.AreEqual(67, result.Evaluate());
    }

    [TestMethod]
    public void TwoPlusTwo()
    {
        IExpression result = Parser.Parse([Lexeme.Number("2"), L.Add, Lexeme.Number("2")]);
        AssertSimpleTree(result, 2, LexemeID.Add, 2);
    }

    [TestMethod]
    public void TwoMinusTwo()
    {
        IExpression result = Parser.Parse([Lexeme.Number("2"), L.Sub, Lexeme.Number("2")]);
        AssertSimpleTree(result, 2, LexemeID.Subtract, 2);
    }

    [TestMethod]
    public void TwoTimesTwo()
    {
        IExpression result = Parser.Parse([Lexeme.Number("2"), L.Mult, Lexeme.Number("2")]);
        AssertSimpleTree(result, 2, LexemeID.Multiply, 2);
    }

    [TestMethod]
    public void TwoDividedByTwo()
    {
        IExpression result = Parser.Parse([Lexeme.Number("2"), L.Div, Lexeme.Number("2")]);
        AssertSimpleTree(result, 2, LexemeID.Divide, 2);
    }

    [TestMethod]
    public void TwoToTheFifthPower()
    {
        IExpression result = Parser.Parse([Lexeme.Number("2"), L.Exp, Lexeme.Number("5")]);
        AssertSimpleTree(result, 2, LexemeID.Exponent, 5);
    }

    [TestMethod]
    public void AddAndMult()
    {
        IExpression result = Parser.Parse([Lexeme.Number("5"), L.Add, Lexeme.Number("5"), L.Mult, Lexeme.Number("2")]);
        Assert.AreEqual(LexemeID.Add, result.ID);

        IExpression[] children = result.Children().ToArray();
        Assert.HasCount(2, children);
        Assert.AreEqual(LexemeID.Number, children[0].ID);
        Assert.AreEqual(5, children[0].Evaluate());
        AssertSimpleTree(children[1], 5, LexemeID.Multiply, 2);
    }

    [TestMethod]
    public void ReorderedAddAndMult() {
        IExpression result = Parser.Parse([L.Open, Lexeme.Number("5"), L.Add, Lexeme.Number("5"), L.Close, L.Mult, Lexeme.Number("2")]);
        Assert.AreEqual(LexemeID.Multiply, result.ID);

        IExpression[] children = result.Children().ToArray();
        Assert.HasCount(2, children);

        AssertSimpleTree(children[0], 5, LexemeID.Add, 5);
        Assert.AreEqual(LexemeID.Number, children[1].ID);
        Assert.AreEqual(2, children[1].Evaluate());
    }

    // I'm not sure how useful this test is, but it's cool regardless.
    [TestMethod]
    public void ManyAdds() {
        IExpression result = Parser.Parse([
            Lexeme.Number("1"),
            L.Add,
            Lexeme.Number("2"),
            L.Add,
            Lexeme.Number("3"),
            L.Add,
            Lexeme.Number("4"),
            L.Add,
            Lexeme.Number("5"),
            L.Add,
            Lexeme.Number("6"),
            L.Add,
            Lexeme.Number("7"),
            L.Add,
            Lexeme.Number("8")]);

        Assert.AreEqual(LexemeID.Add, result.ID);
        IExpression node = result;
        foreach(var expectedRight in new decimal[] {8, 7, 6, 5, 4, 3}) {
            IExpression[] children = node.Children().ToArray();
            Assert.HasCount(2, children);
            Assert.AreEqual(LexemeID.Number, children[1].ID);
            Assert.AreEqual(expectedRight, children[1].Evaluate());

            node = children[0];
        }

        AssertSimpleTree(node, 1, LexemeID.Add, 2);
    }

    [TestMethod]
    public void ExponentsHeckYeah() {
        IExpression node = Parser.Parse([Lexeme.Number("12"), L.Sub, Lexeme.Number("3"), L.Mult, Lexeme.Number("3"), L.Exp, Lexeme.Number("2")]);

        Assert.AreEqual(LexemeID.Subtract, node.ID);
        {
            IExpression[] children = node.Children().ToArray();
            Assert.HasCount(2, children);
            Assert.AreEqual(LexemeID.Number, children[0].ID);
            Assert.AreEqual(12, children[0].Evaluate());
            node = children[1];
        }

        Assert.AreEqual(LexemeID.Multiply, node.ID);
        {
            IExpression[] children = node.Children().ToArray();
            Assert.HasCount(2, children);
            Assert.AreEqual(LexemeID.Number, children[0].ID);
            Assert.AreEqual(3, children[0].Evaluate());
            node = children[1];
        }

        AssertSimpleTree(node, 3, LexemeID.Exponent, 2);
    }

    [TestMethod]
    public void MultDivMult() {
        IExpression node = Parser.Parse([Lexeme.Number("6"), L.Mult, Lexeme.Number("6"), L.Div, Lexeme.Number("6"), L.Mult, Lexeme.Number("6")]);

        Assert.AreEqual(LexemeID.Multiply, node.ID);
        {
            IExpression[] children = node.Children().ToArray();
            Assert.HasCount(2, children);
            Assert.AreEqual(LexemeID.Number, children[1].ID);
            Assert.AreEqual(6, children[1].Evaluate());
            node = children[0];
        }

        Assert.AreEqual(LexemeID.Divide, node.ID);
        {
            IExpression[] children = node.Children().ToArray();
            Assert.HasCount(2, children);
            Assert.AreEqual(LexemeID.Number, children[1].ID);
            Assert.AreEqual(6, children[1].Evaluate());
            node = children[0];
        }

        AssertSimpleTree(node, 6, LexemeID.Multiply, 6);
    }
}

[TestClass]
public sealed class LexerTests
{
    private void AssertArraysEqual<T>(T[] result, T[] expected)
    {
        Assert.HasCount(expected.Length, result);
        for (int i = 0; i < result.Length; i++)
        {
            Assert.AreEqual(result[i], expected[i]);
        }
    }

    private void DidItTwoPlusTwo(Lexeme[] testOn)
    {
        AssertArraysEqual(testOn, [Lexeme.Number("2"), L.Add, Lexeme.Number("2")]);
    }

    [TestMethod]
    public void TwoPlusTwo()
    {
        var result = Lexer.Lex("2 + 2").ToArray();
        DidItTwoPlusTwo(result);
    }

    [TestMethod]
    public void TwoPlusTwoCompressed()
    {
        var result = Lexer.Lex("2+2").ToArray();
        DidItTwoPlusTwo(result);
    }

    [TestMethod]
    public void TwoPlusTwoWhitespace()
    {
        var result = Lexer.Lex("    2     + 2       ").ToArray();
        DidItTwoPlusTwo(result);
    }

    [TestMethod]
    public void TwoPlusTwoTabs()
    {
        var result = Lexer.Lex("\t2\t+\t2\t").ToArray();
        DidItTwoPlusTwo(result);
    }

    [TestMethod]
    public void TwoPlusPositiveTwo()
    {
        var result = Lexer.Lex("2++2").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("2"), L.Add, Lexeme.Number("+2")]);
    }

    [TestMethod]
    public void BrokenTwoPlusPositiveTwo()
    {
        var result = Lexer.Lex("2 ++ 2").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("2"), L.Add, L.Add, Lexeme.Number("2")]);
    }

    [TestMethod]
    public void SubtractThing()
    {
        var result = Lexer.Lex("69-420").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("69"), L.Sub, Lexeme.Number("420")]);
    }

    [TestMethod]
    public void DoubleSubtractThing()
    {
        var result = Lexer.Lex("69--420").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("69"), L.Sub, Lexeme.Number("-420")]);
    }

    [TestMethod]
    public void BrokenDoubleSubtractThing()
    {
        var result = Lexer.Lex("69 -- 420").ToArray();
        AssertArraysEqual(result, [Lexeme.Number("69"), L.Sub, L.Sub, Lexeme.Number("420")]);
    }

    [TestMethod]
    public void OperatorSpam()
    {
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
    public void LongBar()
    {
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
    public void Stars()
    {
        var result = Lexer.Lex("***********").ToArray();
        AssertArraysEqual(result, [L.Exp, L.Exp, L.Exp, L.Exp, L.Exp, L.Mult]);
    }

    [TestMethod]
    public void ParenthesisSpam()
    {
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
