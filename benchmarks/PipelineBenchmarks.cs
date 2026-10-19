using BenchmarkDotNet.Attributes;
using Helpers;

[MemoryDiagnoser]
public class PipelineBenchmarks
{
  [ParamsAllValues]
  public InputKind Input { get; set; }

  private string source = null!;
  private Target target;

  [GlobalSetup]
  public void Setup()
  {
    source = Inputs.Create(Input);
    target = OperatingSystem.IsMacOS() ? Target.Mac : Target.Linux;
  }

  [Benchmark]
  public string Compile() => Emitter.Run(Generator.Run(Parser.Run(Lexer.Run(source))), target);
}
