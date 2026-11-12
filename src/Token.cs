public readonly union Token
(
  Identifier,
  Int,
  Void,
  Return,
  Constant,
  OpenParenthesis,
  CloseParenthesis,
  OpenBrace,
  CloseBrace,
  Semicolon,
  Negate,
  Decrement,
  Complement
);

public record Identifier(string Value);
public record Int();
public record Void();
public record Return();
public record Constant(int Value);
public record OpenParenthesis();
public record CloseParenthesis();
public record OpenBrace();
public record CloseBrace();
public record Semicolon();
public record Negate();
public record Decrement();
public record Complement();
