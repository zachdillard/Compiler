using Helpers;
using System.Text.RegularExpressions;

public partial class Lexer
{
  public static List<Token> Run(string input)
  {
    List<Token> tokens = [];

    while (input != string.Empty)
    {
      input = input.TrimStart();
      if (input == string.Empty)
        break;

      bool matched = false;

      foreach ((Regex pattern, Func<string, Token> create) in Patterns)
      {
        Match match = pattern.Match(input);
        if (!match.Success || match.Index != 0)
          continue;

        tokens.Add(create(match.Value));
        input = input[match.Length..];
        matched = true;
        break;
      }

      if (!matched)
        throw new Error($"Unexpected character: '{input[0]}'");
    }

    return tokens;
  }

  private static readonly (Regex Pattern, Func<string, Token> Create)[] Patterns =
  [
    (IntRegex, _ => new Int()),
    (VoidRegex, _ => new Void()),
    (ReturnRegex, _ => new Return()),
    (IdentifierRegex, text => new Identifier(text)),
    (ConstantRegex, text => new Constant(ParseConstant(text))),
    (OpenParenthesisRegex, _ => new OpenParenthesis()),
    (CloseParenthesisRegex, _ => new CloseParenthesis()),
    (OpenBraceRegex, _ => new OpenBrace()),
    (CloseBraceRegex, _ => new CloseBrace()),
    (SemicolonRegex, _ => new Semicolon())
  ];

  private static int ParseConstant(string text)
  {
    if (int.TryParse(text, out int value))
      return value;

    throw new Error("integer constant is outside the supported int range.");
  }

  [GeneratedRegex(@"\bint\b")]
  private static partial Regex IntRegex { get; }

  [GeneratedRegex(@"\bvoid\b")]
  private static partial Regex VoidRegex { get; }

  [GeneratedRegex(@"\breturn\b")]
  private static partial Regex ReturnRegex { get; }

  [GeneratedRegex(@"[a-zA-Z_]\w*\b")]
  private static partial Regex IdentifierRegex { get; }

  [GeneratedRegex(@"[0-9]+\b")]
  private static partial Regex ConstantRegex { get; }

  [GeneratedRegex(@"\(")]
  private static partial Regex OpenParenthesisRegex { get; }

  [GeneratedRegex(@"\)")]
  private static partial Regex CloseParenthesisRegex { get; }

  [GeneratedRegex(@"{")]
  private static partial Regex OpenBraceRegex { get; }

  [GeneratedRegex(@"}")]
  private static partial Regex CloseBraceRegex { get; }

  [GeneratedRegex(@";")]
  private static partial Regex SemicolonRegex { get; }
}
