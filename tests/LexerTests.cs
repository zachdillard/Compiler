using Helpers;
using Xunit;

namespace CompilerIntegrationTests;

public sealed class LexerTests
{
  [Theory]
  [InlineData("int main(void) { return 42; }")]
  [InlineData("int main(void){return 42;}")]
  public void Run_ValidSource_ReturnsExpectedTokens(string source)
  {
    List<Token> tokens = Lexer.Run(source);

    Assert.Collection(tokens,
      token => Assert.True(token is Int),
      token => Assert.True(token is Identifier { Value: "main" }),
      token => Assert.True(token is OpenParenthesis),
      token => Assert.True(token is Void),
      token => Assert.True(token is CloseParenthesis),
      token => Assert.True(token is OpenBrace),
      token => Assert.True(token is Return),
      token => Assert.True(token is Constant { Value: 42 }),
      token => Assert.True(token is Semicolon),
      token => Assert.True(token is CloseBrace));
  }

  [Fact]
  public void Run_MixedWhitespace_ReturnsExpectedTokens()
  {
    List<Token> tokens = Lexer.Run(" \t\r\nint\tmain (\nvoid\r)\f{\vreturn\t42 ; } \n");

    Assert.Collection(tokens,
      token => Assert.True(token is Int),
      token => Assert.True(token is Identifier { Value: "main" }),
      token => Assert.True(token is OpenParenthesis),
      token => Assert.True(token is Void),
      token => Assert.True(token is CloseParenthesis),
      token => Assert.True(token is OpenBrace),
      token => Assert.True(token is Return),
      token => Assert.True(token is Constant { Value: 42 }),
      token => Assert.True(token is Semicolon),
      token => Assert.True(token is CloseBrace));
  }

  [Theory]
  [InlineData("int_value")]
  [InlineData("voided")]
  [InlineData("return2")]
  [InlineData("_entry1")]
  [InlineData("Int")]
  public void Run_KeywordPrefixes_ReturnsIdentifiers(string name)
  {
    Token token = Assert.Single(Lexer.Run(name));

    Assert.True(token is Identifier identifier && identifier.Value == name);
  }

  [Fact]
  public void Run_EmptyInput_ReturnsNoTokens()
  {
    Assert.Empty(Lexer.Run(string.Empty));
  }

  [Fact]
  public void Run_WhitespaceInput_ReturnsNoTokens()
  {
    Assert.Empty(Lexer.Run(" \t\r\n\f\v"));
  }

  [Theory]
  [InlineData("0", 0)]
  [InlineData("42", 42)]
  [InlineData("00042", 42)]
  [InlineData("2147483647", int.MaxValue)]
  public void Run_IntegerConstant_ReturnsExpectedValue(string source, int expected)
  {
    Token token = Assert.Single(Lexer.Run(source));

    Assert.True(token is Constant constant && constant.Value == expected);
  }

  [Theory]
  [InlineData("2147483648")]
  [InlineData("999999999999999999999999")]
  public void Run_OverflowingConstant_ThrowsDiagnostic(string source)
  {
    Error exception = Assert.Throws<Error>(() => Lexer.Run(source));

    Assert.Equal("integer constant is outside the supported int range.", exception.Message);
  }

  [Theory]
  [InlineData("@", "Unexpected character: '@'")]
  [InlineData("int main(void){return -1;}", "Unexpected character: '-'")]
  [InlineData("int main(void){return 1+2;}", "Unexpected character: '+'")]
  public void Run_UnsupportedCharacter_ThrowsDiagnostic(string source, string expected)
  {
    Error exception = Assert.Throws<Error>(() => Lexer.Run(source));

    Assert.Equal(expected, exception.Message);
  }

  [Theory]
  [InlineData("123abc")]
  [InlineData("123_abc")]
  public void Run_NumericIdentifier_ThrowsDiagnostic(string source)
  {
    Error exception = Assert.Throws<Error>(() => Lexer.Run(source));

    Assert.Equal("Unexpected character: '1'", exception.Message);
  }
}
