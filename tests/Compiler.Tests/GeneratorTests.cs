using Xunit;

namespace CompilerIntegrationTests;

public sealed class GeneratorTests
{
  [Theory]
  [InlineData("main", 2)]
  [InlineData("answer", 0)]
  [InlineData("_entry1", 42)]
  [InlineData("minimum", int.MinValue)]
  [InlineData("maximum", int.MaxValue)]
  public void GeneratesMoveAndReturn(string identifier, int value)
  {
    C.Program program = new(new C.Function(identifier, new C.Return(new C.Constant(value))));

    Assembly.Program assembly = Generator.Run(program);

    Assert.Equal(identifier, assembly.Function.Identifer);
    Assert.Collection(assembly.Function.Instructions,
      instruction => Assert.True(instruction is Assembly.Mov
      {
        Source: Assembly.Imm { Value: var immediate },
        Destination: Assembly.Register
      } && immediate == value),
      instruction => Assert.True(instruction is Assembly.Ret));
  }
}
