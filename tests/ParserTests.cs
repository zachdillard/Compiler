using Helpers;
using Xunit;
using C = Syntax.C;

namespace CompilerIntegrationTests;

public sealed class ParserTests
{
  [Theory]
  [InlineData("main", 0)]
  [InlineData("_entry1", 42)]
  [InlineData("maximum", int.MaxValue)]
  public void ParsesFunctionAndConsumesTokens(string identifier, int value)
  {
    List<Token> tokens = FunctionTokens(identifier, value);

    C.Program program = Parser.Run(tokens);

    Assert.Equal(new C.Program(new C.Function(identifier, new C.Return(new C.Constant(value)))), program);
    Assert.Empty(tokens);
  }

  [Fact]
  public void ParsesNegation()
  {
    Assert.Equal<C.Expression>(new C.Unary(new C.Negate(), new C.Constant(42)),
      ParseReturnExpression("-42"));
  }

  [Fact]
  public void ParsesComplement()
  {
    Assert.Equal<C.Expression>(new C.Unary(new C.Complement(), new C.Constant(42)),
      ParseReturnExpression("~42"));
  }

  [Fact]
  public void ParsesNestedUnaryOperators()
  {
    C.Expression expected = new C.Unary(new C.Negate(),
      new C.Unary(new C.Complement(), new C.Constant(42)));

    Assert.Equal(expected, ParseReturnExpression("-~(42)"));
  }

  [Fact]
  public void ParsesRedundantParenthesesAsConstant()
  {
    Assert.Equal<C.Expression>(new C.Constant(42), ParseReturnExpression("((42))"));
  }

  [Theory]
  [InlineData("--42", "Expected an expression.")]
  [InlineData("-~", "Expected an expression.")]
  [InlineData("(42", "Expected CloseParenthesis.")]
  [InlineData("42)", "Expected Semicolon.")]
  public void RejectsInvalidExpressionsWithDiagnostic(string expression, string expected)
  {
    List<Token> tokens = Lexer.Run($"int main(void) {{ return {expression}; }}");

    Error exception = Assert.Throws<Error>(() => Parser.Run(tokens));

    Assert.Equal(expected, exception.Message);
  }

  [Theory]
  [InlineData("")]
  [InlineData("int main(void) { return 2;")]
  [InlineData("int main(void) { return 2 }")]
  [InlineData("int main() { return 2; }")]
  [InlineData("int 2(void) { return 2; }")]
  [InlineData("int main(void) { return main; }")]
  [InlineData("int main(void) { return 2; } int extra(void) { return 3; }")]
  public void RejectsInvalidSyntax(string source)
  {
    List<Token> tokens = Lexer.Run(source);

    Assert.Throws<Error>(() => Parser.Run(tokens));
  }

  [Theory]
  [InlineData(0)]
  [InlineData(1)]
  [InlineData(2)]
  [InlineData(3)]
  [InlineData(4)]
  [InlineData(5)]
  [InlineData(6)]
  [InlineData(7)]
  [InlineData(8)]
  [InlineData(9)]
  public void RejectsTruncatedTokensWithDiagnostic(int length)
  {
    List<Token> tokens = FunctionTokens("main", 42).GetRange(0, length);

    Error exception = Assert.Throws<Error>(() => Parser.Run(tokens));

    Assert.Equal("Unexpected end of input.", exception.Message);
  }

  [Theory]
  [InlineData(0, "Expected Int.")]
  [InlineData(1, "Expected an identifier.")]
  [InlineData(2, "Expected OpenParenthesis.")]
  [InlineData(3, "Expected Void.")]
  [InlineData(4, "Expected CloseParenthesis.")]
  [InlineData(5, "Expected OpenBrace.")]
  [InlineData(6, "Expected Return.")]
  [InlineData(7, "Expected an expression.")]
  [InlineData(8, "Expected Semicolon.")]
  [InlineData(9, "Expected CloseBrace.")]
  public void RejectsIncorrectTokenWithDiagnostic(int position, string expected)
  {
    List<Token> tokens = FunctionTokens("main", 42);
    // Semicolon is incorrect everywhere except its own grammar position.
    tokens[position] = position == 8 ? new Int() : new Semicolon();

    Error exception = Assert.Throws<Error>(() => Parser.Run(tokens));

    Assert.Equal(expected, exception.Message);
  }

  [Theory]
  [InlineData(false)]
  [InlineData(true)]
  public void RejectsTrailingTokensWithDiagnostic(bool secondFunction)
  {
    List<Token> tokens = FunctionTokens("main", 42);
    if (secondFunction)
      tokens.AddRange(FunctionTokens("other", 0));
    else
      tokens.Add(new Semicolon());

    Error exception = Assert.Throws<Error>(() => Parser.Run(tokens));

    Assert.Equal("Unexpected tokens after the function.", exception.Message);
  }

  private static C.Expression ParseReturnExpression(string expression)
  {
    List<Token> tokens = Lexer.Run($"int main(void) {{ return {expression}; }}");

    C.Program program = Parser.Run(tokens);

    Assert.Empty(tokens);
    return program.Function.Statement.Expression;
  }

  private static List<Token> FunctionTokens(string identifier, int value) =>
  [
    new Int(), new Identifier(identifier), new OpenParenthesis(), new Void(),
    new CloseParenthesis(), new OpenBrace(), new Return(), new Constant(value),
    new Semicolon(), new CloseBrace()
  ];
}
