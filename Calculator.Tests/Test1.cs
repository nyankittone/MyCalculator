namespace Calculator.Tests;

// Handing off L's to this whole codebase
static class L
{
    public static Lexeme Add = new Lexeme(LexemeID.Add, "+");
    public static Lexeme Sub = new Lexeme(LexemeID.Subtract, "-");
    public static Lexeme Mult = new Lexeme(LexemeID.Multiply, "*");
    public static Lexeme Div = new Lexeme(LexemeID.Divide, "/");
    public static Lexeme Exp = new Lexeme(LexemeID.Exponent, "**");
    public static Lexeme Open = new Lexeme(LexemeID.IncPrecedence, "(");
    public static Lexeme Close = new Lexeme(LexemeID.DecPrecedence, ")");

    public static Lexeme Constant(string stuff) => new Lexeme(LexemeID.Constant, stuff);
    public static Lexeme Variable(string stuff) => new Lexeme(LexemeID.Variable, stuff);
    public static Lexeme Builtin(string stuff) => new Lexeme(LexemeID.BuiltinFunc, stuff);
    public static Lexeme CustomFunc(string stuff) => new Lexeme(LexemeID.CustomFunc, stuff);
    public static Lexeme Inval(string stuff) => new Lexeme(LexemeID.Invalid, stuff);
    public static Lexeme Num(string n) => new Lexeme(LexemeID.Number, n);

    // TODO: Fix this code so that the lexemes spat out have fake indices for them
    public static SequentialLexeme[] Seq(ILexeme[] input)
    {
        LexemeSpawner spawn = new();
        // IEnumerable<SequentialLexeme> thing = from item in input select spawn.ChangeSequence(item);
        int index = 0;
        IEnumerable<SequentialLexeme> thing = input.Select((item, i) =>
        {
            int oldIndex = index;
            index += item.Token.Length + 1;
            return new SequentialLexeme(new Lexeme(item.ID, item.Token), oldIndex, (uint)i);
        });
        return thing.ToArray();
    }

    public static ParserExceptionFactory GimmeFactory(IEnumerable<SequentialLexeme> stream)
    {
        string inferredString = String.Join(" ", stream);
        return new ParserExceptionFactory(inferredString);
    }
}

[TestClass]
public sealed class ParserTests
{
    private void AssertExpr(IExpression? expression, LexemeID id)
    {
        Assert.IsNotNull(expression);
        Assert.AreEqual(id, (expression!).ID);
    }

    private void AssertNumber(IExpression? expression, in decimal number)
    {
        AssertExpr(expression, LexemeID.Number);
        Assert.AreEqual(number, (expression!).Evaluate());
    }

    private void AssertSimpleTree(
        IExpression? expr, decimal expectedLeft, LexemeID expectedOperator, decimal expectedRight
    )
    {
        Assert.IsNotNull(expr);
        var existantExpr = expr!;
        Assert.AreEqual(expectedOperator, existantExpr.ID);
        IExpression[] children = existantExpr.Children().ToArray();
        Assert.HasCount(2, children);

        AssertNumber(children[0], expectedLeft);
        AssertNumber(children[1], expectedRight);
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
        SequentialLexeme[] input = L.Seq([L.Num("67")]);
        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertNumber(result, 67);
    }

    [TestMethod]
    public void TwoPlusTwo()
    {
        var input = L.Seq([L.Num("2"), L.Add, L.Num("2")]);
        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertSimpleTree(result, 2, LexemeID.Add, 2);
    }

    [TestMethod]
    public void TwoMinusTwo()
    {
        var input = L.Seq([L.Num("2"), L.Sub, L.Num("2")]);
        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertSimpleTree(result, 2, LexemeID.Subtract, 2);
    }

    [TestMethod]
    public void TwoTimesTwo()
    {
        var input = L.Seq([L.Num("2"), L.Mult, L.Num("2")]);
        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertSimpleTree(result, 2, LexemeID.Multiply, 2);
    }

    [TestMethod]
    public void TwoDividedByTwo()
    {
        var input = L.Seq([L.Num("2"), L.Div, L.Num("2")]);
        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertSimpleTree(result, 2, LexemeID.Divide, 2);
    }

    [TestMethod]
    public void TwoToTheFifthPower()
    {
        var input = L.Seq([L.Num("2"), L.Exp, L.Num("5")]);
        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertSimpleTree(result, 2, LexemeID.Exponent, 5);
    }

    [TestMethod]
    public void AddAndMult()
    {
        var input = L.Seq([L.Num("5"), L.Add, L.Num("5"), L.Mult, L.Num("2")]);
        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertExpr(result, LexemeID.Add);

        IExpression[] children = (result!).Children().ToArray();
        Assert.HasCount(2, children);
        Assert.AreEqual(LexemeID.Number, children[0].ID);
        Assert.AreEqual(5, children[0].Evaluate());
        AssertSimpleTree(children[1], 5, LexemeID.Multiply, 2);
    }

    [TestMethod]
    public void ReorderedAddAndMult()
    {
        var input = L.Seq([L.Open, L.Num("5"), L.Add, L.Num("5"), L.Close, L.Mult, L.Num("2")]);
        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertExpr(result, LexemeID.Multiply);

        IExpression[] children = (result!).Children().ToArray();
        Assert.HasCount(2, children);

        AssertSimpleTree(children[0], 5, LexemeID.Add, 5);
        Assert.AreEqual(LexemeID.Number, children[1].ID);
        Assert.AreEqual(2, children[1].Evaluate());
    }

    // I'm not sure how useful this test is, but it's cool regardless.
    [TestMethod]
    public void ManyAdds()
    {
        var input = L.Seq([
            L.Num("1"),
            L.Add,
            L.Num("2"),
            L.Add,
            L.Num("3"),
            L.Add,
            L.Num("4"),
            L.Add,
            L.Num("5"),
            L.Add,
            L.Num("6"),
            L.Add,
            L.Num("7"),
            L.Add,
            L.Num("8")]);

        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertExpr(result, LexemeID.Add);

        IExpression node = result!;
        foreach (var expectedRight in new decimal[] { 8, 7, 6, 5, 4, 3 })
        {
            IExpression[] children = node.Children().ToArray();
            Assert.HasCount(2, children);
            Assert.AreEqual(LexemeID.Number, children[1].ID);
            Assert.AreEqual(expectedRight, children[1].Evaluate());

            node = children[0];
        }

        AssertSimpleTree(node, 1, LexemeID.Add, 2);
    }

    [TestMethod]
    public void ExponentsHeckYeah()
    {
        var input = L.Seq([L.Num("12"), L.Sub, L.Num("3"), L.Mult, L.Num("3"), L.Exp, L.Num("2")]);
        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertExpr(result, LexemeID.Subtract);
        var node = result!;

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
    public void MultDivMult()
    {
        var input = L.Seq([L.Num("6"), L.Mult, L.Num("6"), L.Div, L.Num("6"), L.Mult, L.Num("6")]);
        IExpression? result = Parser.Parse(input, L.GimmeFactory(input));
        AssertExpr(result, LexemeID.Multiply);
        var node = result!;

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

    public static IEnumerable<SequentialLexeme[]> BadOperatorData => [
        L.Seq([L.Num("9"), L.Inval(";KJ:J;jh;lkjH"), L.Num("10")]),
        L.Seq([L.Num("9"), L.Inval(";KJ:J;jh;lkjH"), L.Num("10"), L.Sub, L.Num("21")]),
        L.Seq([L.Num("9"), L.Add, L.Num("10"), L.Inval(";KJ:J;jh;lkjH"), L.Num("21")]),
    ];

    // TODO: Consider adding error IDs to the ParserExceptions. That way we can assert against those
    // instead of the message text.
    [TestMethod]
    [DynamicData(nameof(BadOperatorData))]
    public void ErrorBadOperator(SequentialLexeme[] input)
    {
        try
        {
            Parser.Parse(input, L.GimmeFactory(input));
        }
        catch (Exception e)
        {
            Assert.IsInstanceOfType<AggregateException>(e);
            var es = ((AggregateException)e).InnerExceptions;

            Assert.HasCount(1, es);
            Assert.IsInstanceOfType<ParserException>(es[0]);
            var pe = ((ParserException)es[0]);
            Assert.AreEqual(ParserErrorID.ExpectedOperator, pe.ID);
            return;
        }

        Assert.Fail("Expected parser exception, ran sucessfully instead");
    }

    public static IEnumerable<SequentialLexeme[]> BadOperandData => [
        L.Seq([L.Num("9"), L.Add, L.Inval("'''''''''''gthyj,."), L.Add, L.Num("10")]),
        L.Seq([L.Num("9"), L.Add, L.Num("10"), L.Mult, L.Inval("'''''''''''gthyj,."), L.Add, L.Num("21")]),
    ];

    [TestMethod]
    [DynamicData(nameof(BadOperandData))]
    public void ErrorBadOperand(SequentialLexeme[] input)
    {
        try
        {
            Parser.Parse(input, L.GimmeFactory(input));
        }
        catch (Exception e)
        {
            Assert.IsInstanceOfType<AggregateException>(e);
            var es = ((AggregateException)e).InnerExceptions;

            Assert.HasCount(1, es);
            Assert.IsInstanceOfType<ParserException>(es[0]);
            var pe = ((ParserException)es[0]);
            Assert.AreEqual(ParserErrorID.ExpectedNumber, pe.ID);
            return;
        }

        Assert.Fail("Expected parser exception, ran sucessfully instead");
    }

    public static IEnumerable<SequentialLexeme[]> EmptyParenthesis => [
        L.Seq([L.Open, L.Close]),
        L.Seq([L.Num("5"), L.Add, L.Open, L.Close]),
        L.Seq([L.Open, L.Close, L.Add, L.Num("5")]),
        L.Seq([L.Open, L.Open, L.Close, L.Close]),
        L.Seq([L.Num("9"), L.Add, L.Open, L.Close, L.Add, L.Num("10")]),
        L.Seq([
            L.Num("1"),
            L.Sub,
            L.Open,
            L.Open,
            L.Open,
            L.Open,
            L.Open,
            L.Close,
            L.Close,
            L.Close,
            L.Close,
            L.Close
        ]),
    ];

    [TestMethod]
    [DynamicData(nameof(EmptyParenthesis))]
    public void TestEmptyParenthesis(SequentialLexeme[] input) {
        try
        {
            Parser.Parse(input, L.GimmeFactory(input));
        }
        catch (Exception e)
        {
            Assert.IsInstanceOfType<AggregateException>(e);
            var es = ((AggregateException)e).InnerExceptions;

            Assert.HasCount(1, es);
            Assert.IsInstanceOfType<ParserException>(es[0]);
            var pe = ((ParserException)es[0]);
            Assert.AreEqual(ParserErrorID.EmptyParenthesis, pe.ID);
            return;
        }

        Assert.Fail("Expected parser exception, ran sucessfully instead");
    }

    public static IEnumerable<(SequentialLexeme[], int[])> UnclosedParenthesis1 => [
        (L.Seq([L.Close]), [0]),
        (L.Seq([L.Close, L.Close, L.Close]), [0, 2, 4]),
        (L.Seq([L.Num("5"), L.Add, L.Close, L.Sub, L.Num("3")]), [4]),
        (L.Seq([L.Num("5"), L.Add, L.Close, L.Close, L.Close, L.Sub, L.Num("3")]), [4, 6, 8]),
        (L.Seq([L.Num("10"), L.Add, L.Close]), [5]),
        (L.Seq([L.Close, L.Add, L.Num("10")]), [0]),
    ];

    [TestMethod]
    [DynamicData(nameof(UnclosedParenthesis1))]
    public void TestUnclosedParenthesis1((SequentialLexeme[], int[]) input) {
        SequentialLexeme[] inputSequence = input.Item1;
        int[] indices = input.Item2;

        try
        {
            Parser.Parse(inputSequence, L.GimmeFactory(inputSequence));
        }
        catch (Exception e)
        {
            Assert.IsInstanceOfType<AggregateException>(e);
            var es = ((AggregateException)e).InnerExceptions;

            Assert.HasCount(indices.Length * 2, es);
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.IsInstanceOfType<ParserException>(es[i * 2]);
                var pe = ((ParserException)es[i * 2]);
                Assert.AreEqual(ParserErrorID.ExpectedNumber, pe.ID);
                Assert.AreEqual(indices[i], pe.Index);

                Assert.IsInstanceOfType<ParserException>(es[i * 2 + 1]);
                var pe2 = ((ParserException)es[i * 2 + 1]);
                Assert.AreEqual(ParserErrorID.ExtraParenthesisClose, pe2.ID);
                Assert.AreEqual(indices[i], pe2.Index);
            }

            return;
        }

        Assert.Fail("Expected parser exception, ran sucessfully instead");
    }

    public static IEnumerable<(SequentialLexeme[], int[])> UnclosedParenthesis2 => [
        (L.Seq([L.Open]), [0]),
        (L.Seq([L.Open, L.Open, L.Open]), [4, 2, 0]),
        (L.Seq([L.Open, L.Num("67")]), [0]),
        (L.Seq([L.Num("9"), L.Add, L.Open]), [4]),
        (L.Seq([L.Num("9"), L.Add, L.Open, L.Num("10")]), [4]),
        (L.Seq([L.Open, L.Open, L.Open, L.Open, L.Open, L.Num("42"), L.Close, L.Close, L.Close, L.Close]), [0]),
        (L.Seq([L.Open, L.Open, L.Open, L.Open, L.Open, L.Num("42"), L.Close, L.Close, L.Close]), [2, 0]),
    ];

    [TestMethod]
    [DynamicData(nameof(UnclosedParenthesis2))]
    public void TestUnclosedParenthesis2((SequentialLexeme[], int[]) input) {
        SequentialLexeme[] lexemes = input.Item1;
        int[] indices = input.Item2;

        try
        {
            Parser.Parse(lexemes, L.GimmeFactory(lexemes));
        }
        catch (Exception e)
        {
            Assert.IsInstanceOfType<AggregateException>(e);
            var es = ((AggregateException)e).InnerExceptions;

            Assert.HasCount(indices.Length, es);
            for (int i = 0; i < indices.Length; i++)
            {
                Assert.IsInstanceOfType<ParserException>(es[i]);
                var pe = ((ParserException)es[i]);
                Assert.AreEqual(ParserErrorID.UnclosedParenthesis, pe.ID);
                Assert.AreEqual(indices[i], pe.Index);
            }

            return;
        }

        Assert.Fail("Expected parser exception, ran sucessfully instead");
    }

    [TestMethod]
    public void OpenOpenClose() {
        SequentialLexeme[] input = L.Seq([L.Open, L.Open, L.Close]);

        try
        {
            Parser.Parse(input, L.GimmeFactory(input));
        }
        catch (Exception e)
        {
            Assert.IsInstanceOfType<AggregateException>(e);
            var es = ((AggregateException)e).InnerExceptions;

            Assert.HasCount(2, es);

            Assert.IsInstanceOfType<ParserException>(es[0]);
            var ex1 = (ParserException)es[0];
            Assert.AreEqual(ParserErrorID.EmptyParenthesis, ex1.ID);
            Assert.AreEqual(2, ex1.Index);

            Assert.IsInstanceOfType<ParserException>(es[1]);
            var ex2 = (ParserException)es[1];
            Assert.AreEqual(ParserErrorID.UnclosedParenthesis, ex2.ID);
            Assert.AreEqual(0, ex2.Index);
            return;
        }

        Assert.Fail("Expected parser exception, ran sucessfully instead");
    }

    [TestMethod]
    public void OpenCloseClose() {
        SequentialLexeme[] input = L.Seq([L.Open, L.Close, L.Close]);

        try
        {
            Parser.Parse(input, L.GimmeFactory(input));
        }
        catch (Exception e)
        {
            Assert.IsInstanceOfType<AggregateException>(e);
            var es = ((AggregateException)e).InnerExceptions;

            Assert.HasCount(3, es);

            Assert.IsInstanceOfType<ParserException>(es[0]);
            var ex = (ParserException)es[0];
            Assert.AreEqual(ParserErrorID.EmptyParenthesis, ex.ID);
            Assert.AreEqual(0, ex.Index);

            Assert.IsInstanceOfType<ParserException>(es[1]);
            ex = (ParserException)es[1];
            Assert.AreEqual(ParserErrorID.ExpectedOperator, ex.ID);
            Assert.AreEqual(4, ex.Index);

            Assert.IsInstanceOfType<ParserException>(es[2]);
            ex = (ParserException)es[2];
            Assert.AreEqual(ParserErrorID.ExtraParenthesisClose, ex.ID);
            Assert.AreEqual(4, ex.Index);
            return;
        }

        Assert.Fail("Expected parser exception, ran sucessfully instead");
    }
}

[TestClass]
public sealed class LexerTests
{
    private void AssertArraysEqual<T>(T[] result, T[] expected) where T : ILexeme
    {
        Assert.HasCount(expected.Length, result);
        for (int i = 0; i < result.Length; i++)
        {
            if (expected[i].ID != result[i].ID || expected[i].Token != result[i].Token)
            {
                throw new AssertFailedException($"Lexemes mismatch. Expected {expected[i]}, got {result[i]}");
            }
        }
    }

    private void DidItTwoPlusTwo(SequentialLexeme[] testOn)
    {
        AssertArraysEqual(testOn, L.Seq([L.Num("2"), L.Add, L.Num("2")]));
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
        AssertArraysEqual(result, L.Seq([L.Num("2"), L.Add, L.Num("+2")]));
    }

    [TestMethod]
    public void BrokenTwoPlusPositiveTwo()
    {
        var result = Lexer.Lex("2 ++ 2").ToArray();
        AssertArraysEqual(result, L.Seq([L.Num("2"), L.Add, L.Add, L.Num("2")]));
    }

    [TestMethod]
    public void SubtractThing()
    {
        var result = Lexer.Lex("69-420").ToArray();
        AssertArraysEqual(result, L.Seq([L.Num("69"), L.Sub, L.Num("420")]));
    }

    [TestMethod]
    public void DoubleSubtractThing()
    {
        var result = Lexer.Lex("69--420").ToArray();
        AssertArraysEqual(result, L.Seq([L.Num("69"), L.Sub, L.Num("-420")]));
    }

    [TestMethod]
    public void BrokenDoubleSubtractThing()
    {
        var result = Lexer.Lex("69 -- 420").ToArray();
        AssertArraysEqual(result, L.Seq([L.Num("69"), L.Sub, L.Sub, L.Num("420")]));
    }

    [TestMethod]
    public void OperatorSpam()
    {
        var result = Lexer.Lex("+*-///---+-+-/*+**-/+").ToArray();
        var expected = L.Seq([
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
        ]);

        AssertArraysEqual(result, expected);
    }

    [TestMethod]
    public void LongBar()
    {
        var result = Lexer.Lex("8------------3").ToArray();
        AssertArraysEqual(result, L.Seq([
            L.Num("8"),
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
            L.Num("-3"),
        ]));
    }

    [TestMethod]
    public void Stars()
    {
        var result = Lexer.Lex("***********").ToArray();
        AssertArraysEqual(result, L.Seq([L.Exp, L.Exp, L.Exp, L.Exp, L.Exp, L.Mult]));
    }

    [TestMethod]
    public void ParenthesisSpam()
    {
        var result = Lexer.Lex("89(((-7)(+6))))(-4-4(()+67()-69").ToArray();
        AssertArraysEqual(result, L.Seq([
            L.Num("89"),
            L.Open,
            L.Open,
            L.Open,
            L.Num("-7"),
            L.Close,
            L.Open,
            L.Num("+6"),
            L.Close,
            L.Close,
            L.Close,
            L.Close,
            L.Open,
            L.Num("-4"),
            L.Sub,
            L.Num("4"),
            L.Open,
            L.Open,
            L.Close,
            L.Add,
            L.Num("67"),
            L.Open,
            L.Close,
            L.Sub,
            L.Num("69"),
        ]));
    }

    [TestMethod]
    public void InvalidToken()
    {
        var result = Lexer.Lex("69 ;KJ:J;jh;lkjH 10").ToArray();
        AssertArraysEqual(result, L.Seq([
            L.Num("69"),
            L.Inval(";KJ:J;jh;lkjH"),
            L.Num("10"),
        ]));
    }

    [TestMethod]
    public void InvalidTokens()
    {
        var result = Lexer.Lex("9 + bleh47(()vvvvvvvvMEOW**10").ToArray();
        AssertArraysEqual(result, L.Seq([
            L.Num("9"),
            L.Add,
            L.Inval("bleh"),
            L.Num("47"),
            L.Open,
            L.Open,
            L.Close,
            L.Inval("vvvvvvvvMEOW"),
            L.Exp,
            L.Num("10"),
        ]));
    }

    [TestMethod]
    public void SixSevenSymbol() {
        var result = Lexer.Lex("sixseven").ToArray();
        AssertArraysEqual(result, L.Seq([L.Constant("sixseven")]));
    }

    [TestMethod]
    public void SixSevenSymbolWithOtherStuff() {
        var result = Lexer.Lex("5 + sixseven * 2").ToArray();
        AssertArraysEqual(result, L.Seq([
            L.Num("5"),
            L.Add,
            L.Constant("sixseven"),
            L.Mult,
            L.Num("2"),
        ]));
    }

    [TestMethod]
    public void Squirt64() {
        var result = Lexer.Lex("sqrt(64)").ToArray();
        AssertArraysEqual(result, L.Seq([
            L.Builtin("sqrt"),
            L.Open,
            L.Num("64"),
            L.Close,
        ]));
    }
}

