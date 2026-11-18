using Helpers;
using C = Syntax.C;

public class Parser
{
  public static C.Program Run(List<Token> tokens)
  {
    C.Program program = new(ParseFunction(tokens));
    if (tokens.Count != 0)
      throw new Error("Unexpected tokens after the function.");

    return program;
  }

  private static C.Function ParseFunction(List<Token> tokens)
  {
    Expect<Int>(tokens);
    string identifier = ParseIdentifier(tokens);
    Expect<OpenParenthesis>(tokens);
    Expect<Void>(tokens);
    Expect<CloseParenthesis>(tokens);
    Expect<OpenBrace>(tokens);
    C.Statement statement = ParseStatement(tokens);
    Expect<CloseBrace>(tokens);

    return new C.Function(identifier, statement);
  }

  private static C.Statement ParseStatement(List<Token> tokens)
  {
    Expect<Return>(tokens);
    C.Expression expression = ParseExpression(tokens);
    Expect<Semicolon>(tokens);

    return new C.Return(expression);
  }

  private static C.Expression ParseExpression(List<Token> tokens)
  {
    return new C.Constant(ParseInt(tokens));
  }

  private static string ParseIdentifier(List<Token> tokens)
  {
    if (TakeToken(tokens) is Identifier identifier)
      return identifier.Value;

    throw new Error("Expected an identifier.");
  }

  private static int ParseInt(List<Token> tokens)
  {
    if (TakeToken(tokens) is Constant constant)
      return constant.Value;

    throw new Error("Expected an integer constant.");
  }

  private static void Expect<T>(List<Token> tokens)
  {
    if (TakeToken(tokens) is not T)
      throw new Error($"Expected {typeof(T).Name}.");
  }

  private static Token TakeToken(List<Token> tokens)
  {
    if (tokens.Count == 0)
      throw new Error("Unexpected end of input.");

    Token token = tokens[0];
    tokens.RemoveAt(0);
    return token;
  }
}
