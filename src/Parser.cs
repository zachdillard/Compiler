using C = Syntax.C;

public class Parser
{
  public static C.Program Run(List<Token> tokens)
  {
    C.Program program = new(ParseFunction(tokens));
    if (tokens.Count != 0)
      throw new CompileError("Unexpected tokens after the function.");

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
    C.Constant expression = ParseExpression(tokens);
    Expect<Semicolon>(tokens);

    return new C.Return(expression);
  }

  private static C.Constant ParseExpression(List<Token> tokens)
  {
    return new C.Constant(ParseInt(tokens));
  }

  private static string ParseIdentifier(List<Token> tokens)
  {
    if (TakeToken(tokens) is Identifier identifier)
      return identifier.Value;

    throw new CompileError("Expected an identifier.");
  }

  private static int ParseInt(List<Token> tokens)
  {
    if (TakeToken(tokens) is Constant constant)
      return constant.Value;

    throw new CompileError("Expected an integer constant.");
  }

  private static void Expect<T>(List<Token> tokens)
  {
    if (TakeToken(tokens) is not T)
      throw new CompileError($"Expected {typeof(T).Name}.");
  }

  private static Token TakeToken(List<Token> tokens)
  {
    if (tokens.Count == 0)
      throw new CompileError("Unexpected end of input.");

    Token token = tokens[0];
    tokens.RemoveAt(0);
    return token;
  }
}
