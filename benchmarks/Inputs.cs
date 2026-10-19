public enum InputKind
{
  Minimal,
  LongIdentifier,
  Whitespace
}

public static class Inputs
{
  public static string Create(InputKind kind) => kind switch
  {
    InputKind.Minimal => "int main(void) { return 2; }",
    InputKind.LongIdentifier => $"int function_{new string('a', 256)}(void) {{ return 2; }}",
    InputKind.Whitespace => $"int main(void) {{{new string(' ', 4096)}return 2; }}",
    _ => throw new ArgumentOutOfRangeException(nameof(kind))
  };
}
