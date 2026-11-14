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
    C.Return statement = ParseStatement(tokens);
    Expect<CloseBrace>(tokens);

    return new C.Function(identifier, statement);
  }

  private static C.Return ParseStatement(List<Token> tokens)
  {
    Expect<Return>(tokens);
    C.Expression expression = ParseExpression(tokens);
    Expect<Semicolon>(tokens);

    return new C.Return(expression);
  }

  private static C.Expression ParseExpression(List<Token> tokens)
  {
    var nextToken = PeekToken(tokens);

    switch(nextToken)
    {
      case Constant:
        return new C.Constant(ParseInt(tokens));
      case Complement or Negate:
        var op = ParseOperator(tokens);
        var innerExpression = ParseExpression(tokens);
        return new C.Unary(op, innerExpression);
      case OpenParenthesis:
        TakeToken(tokens);
        innerExpression = ParseExpression(tokens);
        Expect<CloseParenthesis>(tokens);
        return innerExpression;
      default:
        throw new Error("Malformed expression");
    }
  }

  private static string ParseIdentifier(List<Token> tokens)
  {
    if (TakeToken(tokens) is Identifier identifier)
      return identifier.Value;

    throw new Error("Expected an identifier.");
  }

  private static C.Operator ParseOperator(List<Token> tokens)
  {
    return TakeToken(tokens) switch
    {
      Complement => new C.Complement(),
      Negate => new C.Negate(),
      _ => throw new Error("Unexpected operator"),
    };
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

    Token token = tokens.First();
    tokens.RemoveAt(0);
    return token;
  }

  private static Token PeekToken(List<Token> tokens)
  {
    if (tokens.Count == 0)
      throw new Error("Unexpected end of input.");

    return tokens.First();
  }
}
