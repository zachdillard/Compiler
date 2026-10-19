using BenchmarkDotNet.Attributes;

[MemoryDiagnoser]
public class LexerBenchmarks
{
  [ParamsAllValues]
  public InputKind Input { get; set; }

  private string source = null!;

  [GlobalSetup]
  public void Setup() => source = Inputs.Create(Input);

  [Benchmark]
  public List<Token> Lex() => Lexer.Run(source);
}
